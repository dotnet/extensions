// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Compliance.Redaction.Tests;

public class HmacRedactorTest
{
    public static IConfigurationSection GetRedactorConfiguration(IConfigurationBuilder builder, int keyId, string key)
    {
        HmacRedactorOptions redactorOptions;

        return builder
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { $"{nameof(HmacRedactorOptions)}:{nameof(redactorOptions.KeyId)}", keyId.ToString(CultureInfo.InvariantCulture) },
                { $"{nameof(HmacRedactorOptions)}:{nameof(redactorOptions.Key)}", key },
            })
            .Build()
            .GetSection(nameof(HmacRedactorOptions));
    }

    [Fact]
    public void HmacHashingRedactor_Misc_Sizes()
    {
        var redactor = new HmacRedactor(Microsoft.Extensions.Options.Options.Create(new HmacRedactorOptions
        {
            Key = HmacExamples[0].Key,
            KeyId = 101
        }));

        for (int i = 1; i < 1024; i++)
        {
            var str = new string('a', i);
            var length = redactor.GetRedactedLength(str);
            var charsWritten = redactor.Redact(str, new char[length]);

            Assert.NotEqual(0, length);
            Assert.Equal(length, charsWritten);
        }

        Assert.Equal(0, redactor.Redact(string.Empty, []));
    }

    [Fact]
    public void HmacHashingRedactor_For_Empty_String_Returns_RedactedSize_Zero()
    {
        var redactor = new HmacRedactor(Microsoft.Extensions.Options.Options.Create(new HmacRedactorOptions
        {
            Key = HmacExamples[0].Key,
            KeyId = 101
        }));

        var length = redactor.GetRedactedLength("");

        Assert.Equal(0, length);
    }

    [Fact]
    public void HmacHashingRedactor_Throws_Argument_Exception_When_Given_Destination_Is_TooSmall()
    {
        var toRedact = Guid.NewGuid().ToString();
        var redactor = new HmacRedactor(Microsoft.Extensions.Options.Options.Create(new HmacRedactorOptions
        {
            Key = HmacExamples[0].Key,
            KeyId = 101
        }));

        var length = redactor.GetRedactedLength(toRedact);
        var tooSmallBuffer = new char[length - 1];

        var e = Record.Exception(() => redactor.Redact(toRedact.AsSpan(), tooSmallBuffer));

        Assert.IsAssignableFrom<ArgumentException>(e);
    }

    [Fact]
    public void HmacHashingRedactor_Throws_When_Null_Options_Provided()
    {
        Assert.Throws<ArgumentException>(() => new HmacRedactor(Microsoft.Extensions.Options.Options.Create<HmacRedactorOptions>(null!)));
    }

    [Fact]
    public void Can_Register_And_Use_HmacRedactor()
    {
        foreach (var example in HmacExamples)
        {
            var redactorProvider = new ServiceCollection()
                .AddRedaction(redaction =>
                {
                    redaction.SetHmacRedactor(options =>
                    {
                        options.Key = example.Key;
                        options.KeyId = example.KeyId;
                    }, FakeClassifications.PrivateData);
                })
                .BuildServiceProvider()
                .GetRequiredService<IRedactorProvider>();

            var redactor = redactorProvider.GetRedactor(FakeClassifications.PrivateData);

            var length = redactor.GetRedactedLength(example.Plaintext);
            var result = redactor.Redact(example.Plaintext);

            Assert.NotNull(result);
            Assert.Equal(result.Length, length);
            Assert.Equal(example.Hash, result);
        }
    }

    [Fact]
    public void GivenHmacRedactorWithConfigurationSectionConfig_RegistersItAsHashingRedactorAndRedacts()
    {
        foreach (var example in HmacExamples)
        {
            var redactorProvider = new ServiceCollection()
                .AddRedaction(redaction =>
                {
                    var section = GetRedactorConfiguration(new ConfigurationBuilder(), example.KeyId, example.Key);
                    redaction.SetHmacRedactor(section, FakeClassifications.PrivateData);
                })
                .BuildServiceProvider()
                .GetRequiredService<IRedactorProvider>();

            var redactor = redactorProvider.GetRedactor(FakeClassifications.PrivateData);

            var length = redactor.GetRedactedLength(example.Plaintext);
            var result = redactor.Redact(example.Plaintext);

            Assert.NotNull(result);
            Assert.Equal(result.Length, length);
            Assert.Equal(example.Hash, result);
        }
    }

    public class HmacExample
    {
        public string Key { get; init; } = string.Empty;
        public int KeyId { get; init; }
        public string Plaintext { get; init; } = string.Empty;
        public string Hash { get; init; } = string.Empty;
    }

#pragma warning disable S103 // Lines should not be too long
#pragma warning disable S3257 // Declarations and initializations should be as concise as possible
    public static readonly HmacExample[] HmacExamples = new HmacExample[]
#pragma warning restore S3257 // Declarations and initializations should be as concise as possible
    {
        new()
        {
            Key = "1ekdO9wLKLNUm8RWFo+Vxw1H3sENf9YbZz4MvSySgGA=",
            KeyId = 101,
            Plaintext = "06377140004912810",
            Hash = "101:ZLpXObbqe3ct474jNO6qgw==",
        },

        new()
        {
            Key = "loUh5nZREM3h3h7BH0nyX/UlNwOTYUPEazn9GyHZvkQ=",
            KeyId = 101,
            Plaintext = "8250238180231812593665236025879",
            Hash = "101:PHsIeg/J+EMUtCpPUkl44g==",
        },

        new()
        {
            Key = "caq7ULwpSS4z7sEKS1OeTZ+RYD2P8E1/gWOgWOuvK+I=",
            KeyId = 101,
            Plaintext = "14205289249528757613587384723531515413219653794252487086716414554858838442569035407288432742643156375379611526756705061369412859161788279451112143087058",
            Hash = "101:9IqU4+6v/lJPtBGTCXKHlA==",
        },

        new()
        {
            Key = "AsHBb7Ct5DQ71s8038ieEcBrovSfOe1p82ZXrwHkvDg=",
            KeyId = 101,
            Plaintext = "233802211132895623295987151039734445725919069973571244842092718753202655377903745071611049025165556355649418838160882514852190077218742860503577690956946404229304814569556645",
            Hash = "101:DPDQhjV9RhaI+s9lHGiPLA==",
        },

        new()
        {
            Key = "rZwxueYOv/Fql/CFWfYnuPN+wg3nPFMfDm39giZI08g=",
            KeyId = 101,
            Plaintext = "847602394600602",
            Hash = "101:MEgoZ3A+OW0sr1tsViar3w==",
        },

        new()
        {
            Key = "0fk80BJ9uCKp3HjKwVh6GajhWq12YQhBLzX/fIVyILI=",
            KeyId = 101,
            Plaintext = "764915826629263203007821477477666683938508358202357382288660519631",
            Hash = "101:DVGwCn9cMalmwXUali2Qyw==",
        },

        new()
        {
            Key = "4AbP8veT9QkR38+jh/apr3HZ7NiM2l/y6jb1A/18zv0=",
            KeyId = 101,
            Plaintext = "38997664313173123968219594428105087247655246493394178493183691123263046562338921350881428745999093310816975649715041640799129192386318565612796179354216581519989880136946868886733246499051299607264580071923026866803521804605488234696677",
            Hash = "101:xZh9JQhDU587uZ3h2MjbAw==",
        },

        new()
        {
            Key = "zQj0OaDz91L/mColA7QyRbGOD/LmDIJhNKmMJ5ZvEH0=",
            KeyId = 101,
            Plaintext = "503809778156513211675916534732740199886897167126543283584067708107372918979137177937103344151933590113399258338677700046085016157646747824424579145584964732950420219086803002127314321978417144703599111923339871211182990790620193999079942844",
            Hash = "101:wJZ6IH2M36uXwQKbidfahw==",
        },

        new()
        {
            Key = "bjpqdzkEuuxfqjSV87LF6U4c9lpQC6aAodTqyrQP6xg=",
            KeyId = 101,
            Plaintext = "69643802508787306087405974674973166304044420666307682216127292986891689706241708345938242088953791814538764739682914297756734502870648499411345974035943550389163872537384009114167838546889736426301771968072104942628972100913739708824519440",
            Hash = "101:qmVBPn9wkELigo8wv7LZiw==",
        },

        new()
        {
            Key = "OzmwgJN29CeRvg9fhbeCox1ePBqF+5CZMSUIXT6XBGw=",
            KeyId = 101,
            Plaintext = "470412699457383461825",
            Hash = "101:mY7p7yqH47WYZP7CnzGDWg==",
        },

        new()
        {
            Key = "NQv4gqzBbB8cvkLiRgIBOP7PeBUQJrL5CWhnAep+zl0=",
            KeyId = 101,
            Plaintext = "6448837006439390412670219502203237214608388561555165178515833832697909033115420609318414341291603819801719718386454",
            Hash = "101:xdpuFzstLboWoR7HWkVa+A==",
        },

        new()
        {
            Key = "KzEV0yDR2BvmyixgluFKTxYsddx5h52VplxAcXztWAQ=",
            KeyId = 101,
            Plaintext = "22448993990866010679075009030029395238262860918126606929987599910716714216458586771720593103874008017816290310172179956518226689936976031314609001275640697969562746636215435431679990213897046408413537055580375998345892514",
            Hash = "101:c0Rvtx8+ySk5uzx3+mf4vw==",
        },

        new()
        {
            Key = "3LIklZBaGKqbMjtLLEm1ypFG5VqKYfho7AoDy/IRhk8=",
            KeyId = 101,
            Plaintext = "6285689764785118294848534030531564631525176221301855417621077624336155000864747885",
            Hash = "101:yo4N1fVW166ujJIOxwx+WA==",
        },

        new()
        {
            Key = "z08LltjVnPdlg28KF7DHDbXJg5JNMlh/NsKXeCznL0c=",
            KeyId = 101,
            Plaintext = "74397821635863509546738304180242368565555711665422472480596209089616501407",
            Hash = "101:lmEDgf/hE8p4yYSnKCTa+Q==",
        },

        new()
        {
            Key = "JB8NMtfTnaF1W79IhVHcHYL3eWKRblyYgnrrWx4a9Sk=",
            KeyId = 101,
            Plaintext = "84992924057788643849345229844532524318015307989712136738859600649633362629483565793825385667308035375386300075329553663031453",
            Hash = "101:Yfo37VRC9GWUMkqG57HgqA==",
        },

        new()
        {
            Key = "I3/c3ii56RETHsq5vAkzPLRmPbyaNmqI8fQ4nHVxEys=",
            KeyId = 101,
            Plaintext = "43287757052171959594272805553019746254624294322",
            Hash = "101:SOP5zMpCSRn9N9/Y3/KPxg==",
        }
    };
#pragma warning restore S103 // Lines should not be too long
}
