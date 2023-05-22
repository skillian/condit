namespace Condit.Tests;

public class StreamTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async ValueTask TestFilterAsync()
    {
        var nums = await new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }
            .ToAsyncEnumerable()
            .Filter(x => x % 2 == 0)
            .ToListAsync();

        Assert.That(nums, Is.EquivalentTo(new [] { 0, 2, 4, 6, 8, }));
    }

    [Test]
    public async ValueTask TestProjectorAsync()
    {
        var nums = await new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }
            .ToAsyncEnumerable()
            .Project(x => x * 2)
            .ToListAsync();

        Assert.That(nums, Is.EquivalentTo(new [] { 0, 2, 4, 6, 8, 10, 12, 14, 16, 18 }));
    }

    [Test]
    public async ValueTask TestJoinerAsync()
    {
        var a = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }.ToAsyncEnumerable();
        var b = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }.ToAsyncEnumerable();
        var nums = await a.InnerJoin(b, (av, bv) => av + bv == 10, (av, bv) => ValueTuple.Create(av, bv))
            .ToListAsync();

        Assert.That(nums, Is.EquivalentTo(new [] {
            (1, 9), (2, 8), (3, 7), (4, 6),
            (5, 5),
            (6, 4), (7, 3), (8, 2), (9, 1)
        }));
    }
}
