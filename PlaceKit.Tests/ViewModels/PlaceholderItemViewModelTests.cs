using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Models;
using PlaceKit.ViewModels;

namespace PlaceKit.Tests.ViewModels
{
    [TestClass]
    public class PlaceholderItemViewModelTests
    {
        [TestMethod]
        public void SettingValue_RaisesValueAndIsModified()
        {
            var item = new PlaceholderItemViewModel(new PlaceholderInfo("Client name", "", 2, false));
            var changed = new List<string>();
            item.PropertyChanged += (sender, e) => changed.Add(e.PropertyName);

            item.Value = "John";

            CollectionAssert.IsSubsetOf(new[] { "Value", "IsModified" }, changed);
            Assert.IsTrue(item.IsModified);
        }

        [TestMethod]
        public void RestoringTheOriginalValue_IsNotAModification()
        {
            var item = new PlaceholderItemViewModel(new PlaceholderInfo("Order", "42", 1, false));

            item.Value = "43";
            item.Value = "42";

            Assert.IsFalse(item.IsModified);
        }

        [TestMethod]
        public void ClearingAnEmptyPlaceholder_IsNotAModification()
        {
            var item = new PlaceholderItemViewModel(new PlaceholderInfo("Order", "", 1, false));

            item.Value = null;

            Assert.IsFalse(item.IsModified);
        }

        [DataTestMethod]
        [DataRow("client", true)]
        [DataRow("SMITH", true)]
        [DataRow("  name ", true)]
        [DataRow("", true)]
        [DataRow(null, true)]
        [DataRow("order", false)]
        public void Matches_SearchesNameAndValue(string search, bool expected)
        {
            var item = new PlaceholderItemViewModel(new PlaceholderInfo("Client name", "John Smith", 1, false));

            Assert.AreEqual(expected, item.Matches(search));
        }

        [TestMethod]
        public void TakeNextOccurrence_Cycles()
        {
            var item = new PlaceholderItemViewModel(new PlaceholderInfo("Client name", "", 3, false));

            int[] taken = { item.TakeNextOccurrence(), item.TakeNextOccurrence(), item.TakeNextOccurrence(), item.TakeNextOccurrence() };

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 0 }, taken);
        }

        [TestMethod]
        public void UsageBadge_OnlyForRepeatedPlaceholders()
        {
            Assert.IsFalse(new PlaceholderItemViewModel(new PlaceholderInfo("A", "", 1, false)).IsUsedSeveralTimes);
            Assert.IsTrue(new PlaceholderItemViewModel(new PlaceholderInfo("A", "", 2, false)).IsUsedSeveralTimes);
        }
    }
}
