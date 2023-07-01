namespace Condit.Tests;

public class ReflectTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void TestClass0()
    {
        Assert.That(
            Condit.Reflect<Class0>.Attributes,
            Is.Empty,
            $"{nameof(Class0)} should have no attributes"
        );
    }

    [Test]
    public void TestClass1Property()
    {
        var attributes = Condit.Reflect<Class1Property>.Attributes;

        Assert.That(
            attributes.Count,
            Is.EqualTo(1),
            $"{nameof(Class1Property)} should have 1 property"
        );

        var longAttribute = (IAttribute<Class1Property, long>)attributes[0];

        Assert.That(
            longAttribute.GetValue(new Class1Property()),
            Is.EqualTo(Class1Property.DEFAULT_PROPERTY_VALUE)
        );

        Assert.That(
            longAttribute,
            Is.Not.TypeOf<IMutableAttribute>(),
            $"{nameof(Class1Property)}.{nameof(Class1Property.Property)} should not be mutable"
        );
    }

    [Test]
    public void TestClass1MutableProperty()
    {
        var attributes = Condit.Reflect<Class1MutableProperty>.Attributes;

        Assert.That(
            attributes.Count,
            Is.EqualTo(1),
            $"{nameof(Class1MutableProperty)} should have 1 property"
        );

        var stringAttribute = (IMutableAttribute<Class1MutableProperty, string>)attributes[0];

        var class1MutableProperty = new Class1MutableProperty();

        Assert.That(
            stringAttribute.GetValue(class1MutableProperty),
            Is.EqualTo(Class1MutableProperty.DEFAULT_MUTABLE_PROPERTY_VALUE)
        );

        const string NEW_VALUE = "test";

        stringAttribute.SetValue(class1MutableProperty, NEW_VALUE);
    
        Assert.That(
            stringAttribute.GetValue(class1MutableProperty),
            Is.EqualTo(NEW_VALUE)
        );
    }

    [Test]
    public void TestClass1Field()
    {
        var attributes = Condit.Reflect<Class1Field>.Attributes;

        Assert.That(
            attributes.Count,
            Is.EqualTo(1),
            $"{nameof(Class1Field)} should have 1 field"
        );

        var dateTimeAttribute = (IAttribute<Class1Field, DateTime>)attributes[0];

        Assert.That(
            dateTimeAttribute.GetValue(new Class1Field()),
            Is.EqualTo(Class1Field.DefaultFieldValue)
        );

        Assert.That(
            dateTimeAttribute,
            Is.Not.TypeOf<IMutableAttribute>(),
            $"{nameof(Class1Field)}.{nameof(Class1Field.Field)} should not be mutable"
        );
    }
}

public class Class0 { }

public class Class1Property
{
    public const long DEFAULT_PROPERTY_VALUE = 123L;
    public long Property => DEFAULT_PROPERTY_VALUE;
}

public class Class1MutableProperty
{
    public const string DEFAULT_MUTABLE_PROPERTY_VALUE = "default mutable property value";

    public string MutableProperty {get;set;} = DEFAULT_MUTABLE_PROPERTY_VALUE;
}

public class Class1Field
{
    public static readonly DateTime DefaultFieldValue = new DateTime(2020, 1, 1);
    public readonly DateTime Field = DefaultFieldValue;
}

public class Class1MutableField
{
    public DateTime Field;
}
