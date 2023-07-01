using System.Linq.Expressions;

namespace Condit.Sql;

class ExpressionBuilder
{
	readonly Dictionary<IQuery, string> tableNames = new Dictionary<IQuery, string>();
	readonly IAsyncCommand asyncCommand;
	readonly Lazy<IParameters> parameters;
	readonly Stack<List<string>> expressionStringStack = new();
	readonly Stack<List<string>> spareLists = new();
	public ExpressionBuilder(IAsyncCommand asyncCommand)
	{
		this.asyncCommand = asyncCommand;
		parameters = new Lazy<IParameters>(() => asyncCommand.AsyncConnection.DriverInfo.CreateParameters(asyncCommand));
	}

	public void AppendExpression(System.Text.StringBuilder stringBuilder, Expression expression)
	{
		new ExpressionVisitor(this).Visit(expression);

		var finalParts = expressionStringStack.Pop();

		stringBuilder.AppendJoin(String.Empty, finalParts);

		PutList(finalParts);
	}

	List<string> GetList() => spareLists.TryPop(out var list)
		? list
		: new List<string>();

	void PutList(List<string> list)
	{
		list.Clear();
		spareLists.Push(list);
	}

	class ExpressionVisitor : System.Linq.Expressions.ExpressionVisitor
	{
		readonly ExpressionBuilder expressionBuilder;

		public ExpressionVisitor(ExpressionBuilder expressionBuilder)
		{
			this.expressionBuilder = expressionBuilder;
		}

		protected override Expression VisitConstant(ConstantExpression node)
		{
			var visited = base.VisitConstant(node);

			if (!(visited is ConstantExpression @const))
				return visited;

			expressionBuilder.expressionStringStack.Push(
				expressionBuilder.GetList().Then(
					list => list.Add(
						expressionBuilder.parameters.Value
							.DefineParameter(@const.Value)
					)
				)
			);

			return visited;
		}

		protected override Expression VisitBinary(BinaryExpression node)
		{
			var visited = base.VisitBinary(node);

			if (!(visited is BinaryExpression binary))
				return visited;

			var right = expressionBuilder.expressionStringStack.Pop();
			var left = expressionBuilder.expressionStringStack.Peek();

			left.Insert(0, "(");

			var infixOperatorIndex = left.Count;
			left.Add(String.Empty); // will be replaced with infix operator
			left.AddRange(right);
			left.Add(")");
			expressionBuilder.PutList(right);

			left[infixOperatorIndex] = binary.NodeType switch {
				ExpressionType.Equal => " = ",
				ExpressionType.NotEqual => " <> ",
				ExpressionType.LessThan => " < ",
				ExpressionType.LessThanOrEqual => " <= ",
				ExpressionType.GreaterThanOrEqual => " >= ",
				ExpressionType.GreaterThan => " > ",
				ExpressionType.Add => " + ",
				ExpressionType.Subtract => " - ",
				ExpressionType.Multiply => " * ",
				ExpressionType.Divide => " / ",
				ExpressionType.And or ExpressionType.AndAlso => " AND ",
				ExpressionType.Or or ExpressionType.OrElse => " OR ",
				_ => throw new Exception($"Unsupported binary operator: {binary.NodeType}")
			};

			return visited;
		}
	}
}