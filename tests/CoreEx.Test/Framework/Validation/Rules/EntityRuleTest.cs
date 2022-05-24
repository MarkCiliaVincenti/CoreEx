﻿using CoreEx.Entities;
using CoreEx.Validation;
using NUnit.Framework;
using System.Threading.Tasks;
using static CoreEx.Test.Framework.Validation.ValidatorTest;

namespace CoreEx.Test.Framework.Validation.Rules
{
    [TestFixture]
    public class EntityRuleTest
    {
        private static readonly IValidatorEx _tiv = Validator.Create<TestItem>().HasProperty(x => x.Code, p => p.Mandatory());
        private static readonly IValidatorEx _tev = Validator.Create<TestEntity>().HasProperty(x => x.Item, p => p.Entity(_tiv));

        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public async Task Validate()
        {
            var te = new TestEntity { Item = new TestItem() };
            var v1 = await te.Validate().Entity(_tev).RunAsync();

            Assert.IsTrue(v1.HasError);
            Assert.AreEqual(1, v1.Messages!.Count);
            Assert.AreEqual("Code is required.", v1.Messages[0].Text);
            Assert.AreEqual(MessageType.Error, v1.Messages[0].Type);
            Assert.AreEqual("Value.Item.Code", v1.Messages[0].Property);
        }
    }
}