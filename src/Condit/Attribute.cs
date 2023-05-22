namespace Condit;

public interface IAttribute
{
	string Name { get; }
	object? GetValue(object instance);
}

public interface IMutableAttribute : IAttribute
{
	void SetValue(object instance, object? value);
}

public interface IAttribute<TValue> : IAttribute
{
	new TValue GetValue(object instance);
}

public interface IMutableAttribute<TValue>
	: IAttribute<TValue>
	, IMutableAttribute
{
	void SetValue(object instance, TValue value);
}

public interface IAttributeOf<TInstance> : IAttribute
{
	object? GetValue(TInstance instance);
}

public interface IMutableAttributeOf<TInstance>
	: IAttributeOf<TInstance>
	, IMutableAttribute
{
	void SetValue(TInstance instance, object? value);
}

public interface IAttribute<TInstance, TValue>
	: IAttribute<TValue>
	, IAttributeOf<TInstance>
{
	new TValue GetValue(TInstance instance);
}

public interface IMutableAttribute<TInstance, TValue>
	: IAttribute<TInstance, TValue>
	, IMutableAttribute<TValue>
	, IMutableAttributeOf<TInstance>
{
	void SetValue(TInstance instance, TValue value);
}

class Property<TInstance, TValue>
	: IAttribute<TInstance, TValue>
{
	public readonly System.Reflection.PropertyInfo PropertyInfo;
	string IAttribute.Name => PropertyInfo.Name;
	public readonly Func<TInstance, TValue> GetValue;
	static readonly Func<TInstance, TValue> DefaultGetValue
		= _ => throw new InvalidOperationException(
			$"Cannot get value of write-only property");

	public Property(System.Reflection.PropertyInfo propertyInfo)
	{
		PropertyInfo = propertyInfo;

		GetValue = propertyInfo.CanRead
			? propertyInfo.GetMethod
				.MustNotBeNull()
				.CreateDelegate<Func<TInstance, TValue>>()
			: DefaultGetValue;
	}

	TValue IAttribute<TInstance, TValue>.GetValue(TInstance instance)
		=> GetValue(instance);

	TValue IAttribute<TValue>.GetValue(object instance)
		=> GetValue((TInstance)instance);

	object? IAttributeOf<TInstance>.GetValue(TInstance instance)
		=> GetValue(instance);

	object? IAttribute.GetValue(object instance)
		=> GetValue((TInstance)instance);
}

class MutableProperty<TInstance, TValue>
	: Property<TInstance, TValue>
	, IMutableAttribute<TInstance, TValue>
{
	public readonly Action<TInstance, TValue> SetValue;

	public MutableProperty(System.Reflection.PropertyInfo propertyInfo)
		: base(propertyInfo)
	{
		SetValue = propertyInfo.CanWrite
			? propertyInfo.SetMethod
				.MustNotBeNull()
				.CreateDelegate<Action<TInstance, TValue>>()
			: throw new ArgumentException(
				message: $"{propertyInfo} must be writable",
				paramName: nameof(propertyInfo)
			);
	}

	void IMutableAttribute<TInstance, TValue>.SetValue(TInstance instance, TValue value)
		=> SetValue(instance, value);

	void IMutableAttribute<TValue>.SetValue(object instance, TValue value)
		=> SetValue((TInstance)instance, value);

	void IMutableAttributeOf<TInstance>.SetValue(TInstance instance, object? value)
		=> SetValue(instance, (TValue)value!);

	void IMutableAttribute.SetValue(object instance, object? value)
		=> SetValue((TInstance)instance, (TValue)value!);
}

class Field<TInstance, TValue>
	: IAttribute<TInstance, TValue>
{
	public readonly System.Reflection.FieldInfo FieldInfo;
	string IAttribute.Name => FieldInfo.Name;
	public readonly Func<TInstance, TValue> GetValue;

	public Field(System.Reflection.FieldInfo fieldInfo, Func<TInstance, TValue> getValue)
	{
		FieldInfo = fieldInfo;
		GetValue = getValue;
	}

	TValue IAttribute<TInstance, TValue>.GetValue(TInstance instance)
		=> GetValue(instance);

	TValue IAttribute<TValue>.GetValue(object instance)
		=> GetValue((TInstance)instance);

	object? IAttributeOf<TInstance>.GetValue(TInstance instance)
		=> GetValue(instance);

	object? IAttribute.GetValue(object instance)
		=> GetValue((TInstance)instance);
}

class MutableField<TInstance, TValue>
	: Field<TInstance, TValue>
	, IMutableAttribute<TInstance, TValue>
{
	public readonly Action<TInstance, TValue> SetValue;

	public MutableField(
		System.Reflection.FieldInfo fieldInfo,
		Func<TInstance, TValue> getValue,
		Action<TInstance, TValue> setValue
	)
		: base(fieldInfo, getValue)
	{
		SetValue = setValue;
	}

	void IMutableAttribute<TInstance, TValue>.SetValue(TInstance instance, TValue value)
		=> SetValue(instance, value);

	void IMutableAttribute<TValue>.SetValue(object instance, TValue value)
		=> SetValue((TInstance)instance, value);

	void IMutableAttributeOf<TInstance>.SetValue(TInstance instance, object? value)
		=> SetValue(instance, (TValue)value!);

	void IMutableAttribute.SetValue(object instance, object? value)
		=> SetValue((TInstance)instance, (TValue)value!);
}
