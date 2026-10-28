using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Models;
using PlaceKit.ViewModels;

namespace PlaceKit.Tests.ViewModels
{
    [TestClass]
    public class ProgressViewModelTests
    {
        [TestMethod]
        public void UnknownTotal_IsIndeterminate()
        {
            var progress = new ProgressViewModel("Loading", () => { });

            Assert.IsTrue(progress.IsIndeterminate);
            Assert.AreEqual("Please wait...", progress.StatusText);
        }

        [TestMethod]
        public void Update_ShowsTheCount()
        {
            var progress = new ProgressViewModel("Loading", () => { });
            var changed = new List<string>();
            progress.PropertyChanged += (sender, e) => changed.Add(e.PropertyName);

            progress.Update(new ProgressInfo(45, 120));

            Assert.IsFalse(progress.IsIndeterminate);
            Assert.AreEqual("45 of 120", progress.StatusText);
            CollectionAssert.Contains(changed, nameof(ProgressViewModel.StatusText));
        }

        [TestMethod]
        public void Update_NeverShowsMoreDoneThanTheTotal()
        {
            var progress = new ProgressViewModel("Loading", () => { });

            progress.Update(new ProgressInfo(12, 10));

            Assert.AreEqual("12 of 12", progress.StatusText);
        }

        [TestMethod]
        public void Cancel_CancelsOnce()
        {
            int cancels = 0;
            var progress = new ProgressViewModel("Loading", () => cancels++);

            progress.CancelCommand.Execute(null);

            Assert.AreEqual(1, cancels);
            Assert.AreEqual("Canceling...", progress.StatusText);
            Assert.IsFalse(progress.CancelCommand.CanExecute(null));
        }
    }
}
