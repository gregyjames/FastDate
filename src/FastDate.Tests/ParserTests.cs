using System.Globalization;
using System.Text;
using AwesomeAssertions;
using Xunit;

namespace FastDate.Tests;

public class ParserTests
{
    [Theory]
    [InlineData("2026-04-12T15:04:05", 2026, 4, 12, 15, 4, 5)]
    [InlineData("0001-01-01T00:00:00", 1, 1, 1, 0, 0, 0)]
    [InlineData("9999-12-31T23:59:59", 9999, 12, 31, 23, 59, 59)]
    [InlineData("1999-12-31T23:59:59", 1999, 12, 31, 23, 59, 59)]
    [InlineData("2000-02-29T12:34:56", 2000, 2, 29, 12, 34, 56)]
    [InlineData("2024-02-29T08:30:45", 2024, 2, 29, 8, 30, 45)]
    [InlineData("1970-01-01T00:00:01", 1970, 1, 1, 0, 0, 1)]
    [InlineData("2026-10-15T23:10:44.1234567Z", 2026, 10, 15, 23, 10, 44)]
    public void FromIso8601_String_ParsesExpectedComponents(
        string iso,
        int expectedYear,
        int expectedMonth,
        int expectedDay,
        int expectedHour,
        int expectedMinute,
        int expectedSecond)
    {
        PackedDateTime packed = Parser.FromIso8601(iso);

        packed.Year().Should().Be(expectedYear);
        packed.Month().Should().Be(expectedMonth);
        packed.Day().Should().Be(expectedDay);
        packed.Hour().Should().Be(expectedHour);
        packed.Minute().Should().Be(expectedMinute);
        packed.Second().Should().Be(expectedSecond);
        (packed.Value & 0xFFUL).Should().Be(0UL);

        DateTime expectedDateTime = new(
            expectedYear,
            expectedMonth,
            expectedDay,
            expectedHour,
            expectedMinute,
            expectedSecond);

        packed.ToDateTime().Should().Be(expectedDateTime);
        packed.ToDateOnly().Should().Be(DateOnly.FromDateTime(expectedDateTime));
    }

    [Theory]
    [InlineData("2026-04-12T15:04:05", 2026, 4, 12, 15, 4, 5)]
    [InlineData("0001-01-01T00:00:00", 1, 1, 1, 0, 0, 0)]
    [InlineData("9999-12-31T23:59:59", 9999, 12, 31, 23, 59, 59)]
    [InlineData("1999-12-31T23:59:59", 1999, 12, 31, 23, 59, 59)]
    [InlineData("2000-02-29T12:34:56", 2000, 2, 29, 12, 34, 56)]
    [InlineData("2024-02-29T08:30:45", 2024, 2, 29, 8, 30, 45)]
    [InlineData("1970-01-01T00:00:01", 1970, 1, 1, 0, 0, 1)]
    [InlineData("2026-10-15T23:10:44.1234567Z", 2026, 10, 15, 23, 10, 44)]
    public void FromIso8601_Utf8Span_MatchesStringParser(
        string iso,
        int expectedYear,
        int expectedMonth,
        int expectedDay,
        int expectedHour,
        int expectedMinute,
        int expectedSecond)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(iso);
        PackedDateTime fromUtf8 = Parser.FromIso8601(utf8);
        PackedDateTime fromString = Parser.FromIso8601(iso);

        fromUtf8.Value.Should().Be(fromString.Value);
        fromUtf8.Year().Should().Be(expectedYear);
        fromUtf8.Month().Should().Be(expectedMonth);
        fromUtf8.Day().Should().Be(expectedDay);
        fromUtf8.Hour().Should().Be(expectedHour);
        fromUtf8.Minute().Should().Be(expectedMinute);
        fromUtf8.Second().Should().Be(expectedSecond);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2026-04-12")]
    [InlineData("2026-04-12T15:04:0")]
    public void FromIso8601_InvalidOrShortString_ThrowsFormatException(string? invalidInput)
    {
        Action act = () => Parser.FromIso8601(invalidInput!);
        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("2026-04-12")]
    [InlineData("2026-04-12T15:04:0")]
    public void FromIso8601_ShortUtf8Span_ThrowsFormatException(string shortInput)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(shortInput);
        Action act = () => Parser.FromIso8601(bytes);
        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(257)]
    [InlineData(512)]
    [InlineData(1000)]
    public void FromIso8601Bulk_VariousBatchSizes_MatchesSingleAndSystemParse(int count)
    {
        DateTime baseDate = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        List<string> inputs = new(count);
        for (int i = 0; i < count; i++)
        {
            DateTime dt = baseDate.AddDays(i * 3).AddSeconds(i * 3723);
            inputs.Add(dt.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
        }

        PackedDateTime[] arrayResult = Parser.FromIso8601Bulk(inputs);
        arrayResult.Should().HaveCount(count);

        PackedDateTime[] spanResult = new PackedDateTime[count];
        Parser.FromIso8601Bulk(inputs, spanResult);

        for (int i = 0; i < count; i++)
        {
            PackedDateTime single = Parser.FromIso8601(inputs[i]);
            arrayResult[i].Value.Should().Be(single.Value);
            spanResult[i].Value.Should().Be(single.Value);

            DateTime expected = DateTime.ParseExact(
                inputs[i],
                "yyyy-MM-ddTHH:mm:ss",
                CultureInfo.InvariantCulture);
            arrayResult[i].ToDateTime().Should().Be(expected);
        }
    }

    [Fact]
    public void FromIso8601Bulk_BoundaryDates_ParsesCorrectly()
    {
        List<string> inputs =
        [
            "0001-01-01T00:00:00",
            "9999-12-31T23:59:59",
            "1999-12-31T23:59:59",
            "2000-02-29T12:34:56",
            "2024-02-29T08:30:45",
            "2026-04-12T15:04:05"
        ];

        PackedDateTime[] results = Parser.FromIso8601Bulk(inputs);

        for (int i = 0; i < inputs.Count; i++)
        {
            DateTime expected = DateTime.ParseExact(
                inputs[i],
                "yyyy-MM-ddTHH:mm:ss",
                CultureInfo.InvariantCulture);
            results[i].ToDateTime().Should().Be(expected);
        }
    }

    [Fact]
    public void FromIso8601Bulk_Validation_ThrowsExpectedExceptions()
    {
        Action nullList = () => Parser.FromIso8601Bulk(null!);
        nullList.Should().Throw<ArgumentNullException>();

        Action nullListSpan = () =>
        {
            Span<PackedDateTime> buf = stackalloc PackedDateTime[1];
            Parser.FromIso8601Bulk(null!, buf);
        };
        nullListSpan.Should().Throw<ArgumentNullException>();

        List<string> twoItems = ["2026-04-12T15:04:05", "2026-04-12T15:04:06"];
        Action tooSmallOutput = () =>
        {
            Span<PackedDateTime> tooSmall = stackalloc PackedDateTime[1];
            Parser.FromIso8601Bulk(twoItems, tooSmall);
        };
        tooSmallOutput.Should().Throw<ArgumentException>();

        List<string> containsShort = ["2026-04-12T15:04:05", "short"];
        Action shortItem = () => Parser.FromIso8601Bulk(containsShort);
        shortItem.Should().Throw<FormatException>();
    }
}
