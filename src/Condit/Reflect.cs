namespace Condit;

public abstract class Reflect
{
	public abstract Type Type { get; }
	public object Default => GetDefault();
	public IReadOnlyList<IAttribute> Attributes => GetAttributes();

	protected static readonly System.Text.RegularExpressions.Regex wordsRegex
		= new System.Text.RegularExpressions.Regex("\\w+");

	static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, Reflect> instances
		= new System.Collections.Concurrent.ConcurrentDictionary<Type, Reflect>();

	public static Reflect Of(Type type)
	{
		if (instances.TryGetValue(type, out var of))
			return of;

		of = (Reflect)Activator
			.CreateInstance(typeof(Reflect<>).MakeGenericType(type))
			.MustNotBeNull();

		return instances.GetOrAdd(type, of);
	}

	protected abstract object GetDefault();
	protected abstract IReadOnlyList<IAttribute> GetAttributes();
}

public class Reflect<TInstance> : Reflect
{
	public override Type Type { get; } = typeof(TInstance);
	new public static IReadOnlyList<IAttributeOf<TInstance>> Attributes { get; }
	[System.Diagnostics.CodeAnalysis.NotNull]
	new public static TInstance Default => defaultFactory()!;
	static readonly Func<TInstance> defaultFactory;
	public static Func<TInstance?, bool> IsNullFunc { get; }
	public static Func<TInstance?, bool> IsNotNullFunc { get; }
	static Reflect()
	{
		Attributes = CreateAttributes();
		defaultFactory = CreateDefault();
		IsNullFunc = CreateIsNullFunc();
		IsNotNullFunc = System.Linq.Expressions.Expression.Parameter(typeof(TInstance))
			.Then(param => System.Linq.Expressions.Expression.Lambda<Func<TInstance?, bool>>(
				System.Linq.Expressions.Expression.Not(
					System.Linq.Expressions.Expression.Invoke(
						System.Linq.Expressions.Expression.Constant(IsNullFunc),
						param
					)
				),
				param
			).Compile());

		IReadOnlyList<IAttributeOf<TInstance>> CreateAttributes()
		{
			var untypedAttributes = typeof(TInstance)
				.GetMembers(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
				.Select(m => m.MemberType switch {
					System.Reflection.MemberTypes.Field => CreateField((System.Reflection.FieldInfo)m),
					System.Reflection.MemberTypes.Property => CreateProperty((System.Reflection.PropertyInfo)m),
					_ => null
				})
				.Where(x => x is not null)
				.ToList()!;

			var typedAttributes = Array.CreateInstance(
				typeof(IAttributeOf<>).MakeGenericType(typeof(TInstance)),
				untypedAttributes.Count
			);

			foreach (var (index, untypedAttribute) in untypedAttributes.Enumerate())
				typedAttributes.SetValue(untypedAttribute, index);

			return (IReadOnlyList<IAttributeOf<TInstance>>)typedAttributes;

			IAttribute CreateField(System.Reflection.FieldInfo fieldInfo)
			{
				var fieldDeclTypeName = fieldInfo
					.DeclaringType.MustNotBeNull()
					.FullName.MustNotBeNull();

				var dynamicMethod = new System.Reflection.Emit.DynamicMethod(String.Concat(
					wordsRegex.Matches(fieldDeclTypeName)
						.Select(m => m.Value)
						.Prepend("Get")
						.Append(fieldInfo.Name)
					),
					fieldInfo.FieldType,
					new [] { fieldInfo.DeclaringType! }
				);

				dynamicMethod.DefineParameter(
					1, System.Reflection.ParameterAttributes.In,
					Functions.ToParameterCase(fieldInfo.DeclaringType!.Name)
				);

				var ilg = dynamicMethod.GetILGenerator();
				ilg.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
				ilg.Emit(System.Reflection.Emit.OpCodes.Ldfld, fieldInfo);
				ilg.Emit(System.Reflection.Emit.OpCodes.Ret);

				var delegateType = typeof(Func<,>).MakeGenericType(typeof(TInstance), fieldInfo.FieldType);

				var getValue = dynamicMethod.CreateDelegate(delegateType);

				if ((fieldInfo.Attributes
					| System.Reflection.FieldAttributes.InitOnly
					| System.Reflection.FieldAttributes.Literal) != default)
				{
					dynamicMethod = new System.Reflection.Emit.DynamicMethod(String.Concat(
						wordsRegex.Matches(fieldDeclTypeName)
							.Select(m => m.Value)
							.Prepend("Set")
							.Append(fieldInfo.Name)
						),
						typeof(void),
						new [] { fieldInfo.DeclaringType!, fieldInfo.FieldType }
					);

					dynamicMethod.DefineParameter(
						1, System.Reflection.ParameterAttributes.In,
						Functions.ToParameterCase(fieldInfo.DeclaringType!.Name)
					);

					dynamicMethod.DefineParameter(
						2, System.Reflection.ParameterAttributes.In,
						Functions.ToParameterCase(fieldInfo.FieldType.Name)
					);

					ilg = dynamicMethod.GetILGenerator();
					ilg.Emit(System.Reflection.Emit.OpCodes.Ldarga_S, (byte)0);
					ilg.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
					ilg.Emit(System.Reflection.Emit.OpCodes.Stfld, fieldInfo);
					ilg.Emit(System.Reflection.Emit.OpCodes.Ret);

					delegateType = typeof(Action<,>).MakeGenericType(typeof(TInstance), fieldInfo.FieldType);

					return (IAttribute)Activator.CreateInstance(
						typeof(MutableField<,>).MakeGenericType(typeof(TInstance), fieldInfo.FieldType),
						new object[] {
							fieldInfo,
							getValue,
							dynamicMethod.CreateDelegate(delegateType)
						}
					).MustNotBeNull();
				}

				return (IAttribute)Activator.CreateInstance(
					typeof(Field<,>).MakeGenericType(typeof(TInstance), fieldInfo.FieldType),
					new object[] {
						fieldInfo,
						getValue
					}
				).MustNotBeNull();
			}

			IAttribute CreateProperty(System.Reflection.PropertyInfo propertyInfo)
				=> (IAttribute)(propertyInfo.CanWrite
					? Activator.CreateInstance(
						typeof(MutableProperty<,>).MakeGenericType(typeof(TInstance), propertyInfo.PropertyType),
						new [] { propertyInfo }
					)
					: Activator.CreateInstance(
						typeof(Property<,>).MakeGenericType(typeof(TInstance), propertyInfo.PropertyType),
						new [] { propertyInfo }
					)
				).MustNotBeNull();
		}

		Func<TInstance> CreateDefault()
		{
			if (typeof(TInstance) == typeof(string))
				return System.Linq.Expressions.Expression.Lambda<Func<TInstance>>(
					System.Linq.Expressions.Expression.Constant(String.Empty)
				).Compile();

			return System.Linq.Expressions.Expression.Lambda<Func<TInstance>>(
					System.Linq.Expressions.Expression.New(
						typeof(TInstance)
							.GetConstructor(Type.EmptyTypes)
							.MustNotBeNull()
					)
				).Compile();
		}
	
		Func<TInstance?, bool> CreateIsNullFunc()
		{
			if (typeof(TInstance).IsValueType)
			{
				if (Nullable.GetUnderlyingType(typeof(TInstance)) is not null)
				{
					var param = System.Linq.Expressions.Expression.Parameter(typeof(TInstance));

					return System.Linq.Expressions.Expression.Lambda<Func<TInstance?, bool>>(
						System.Linq.Expressions.Expression.MakeMemberAccess(
							param,
							typeof(Nullable<>)
								.MakeGenericType(typeof(TInstance))
								.GetProperty(nameof(Nullable<int>.HasValue))
								.MustNotBeNull()
						),
						param
					).Compile();
				}

				return _ => false;
			}

			return x => x is null;
		}
	}

	protected override object GetDefault() => Reflect<TInstance>.Default;
	protected override IReadOnlyList<IAttribute> GetAttributes() => Attributes;
}
