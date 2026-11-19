using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Models;
using PlaceKit.Tests.Fakes;
using PlaceKit.ViewModels;

namespace PlaceKit.Tests.ViewModels
{
    [TestClass]
    public class WelcomeViewModelTests
    {
        readonly FakeAiService _ai = new FakeAiService { HasApiKey = false };
        readonly FakeUrlLauncher _urls = new FakeUrlLauncher();
        readonly List<bool?> _closes = new List<bool?>();

        WelcomeViewModel Create()
        {
            var welcome = new WelcomeViewModel(_ai, _urls, TestProduct.Create());
            welcome.CloseRequested += (sender, e) => _closes.Add(e.DialogResult);
            return welcome;
        }

        [TestMethod]
        public void OpenSample_ClosesWithYes()
        {
            Create().OpenSampleCommand.Execute(null);

            CollectionAssert.AreEqual(new bool?[] { true }, _closes);
        }

        [TestMethod]
        public void SetUpAi_ShowsTheConnectionAfterwards()
        {
            WelcomeViewModel welcome = Create();
            var changed = new List<string>();
            ((INotifyPropertyChanged)welcome).PropertyChanged += (sender, e) => changed.Add(e.PropertyName);
            Assert.IsTrue(welcome.CanSetUpAi);
            _ai.OnSetUpKey = () => _ai.HasApiKey = true;
            _ai.Provider = AiProvider.OpenAI;

            welcome.SetUpAiCommand.Execute(null);

            Assert.AreEqual(1, _ai.SetUps);
            Assert.IsFalse(welcome.CanSetUpAi);
            Assert.AreEqual("ChatGPT is connected.", welcome.AiStatus);
            CollectionAssert.Contains(changed, nameof(WelcomeViewModel.CanSetUpAi));
            Assert.AreEqual(0, _closes.Count, "the welcome stays open");
        }

        [TestMethod]
        public void SetUpAi_IsNotOfferedWithAKey()
        {
            _ai.HasApiKey = true;

            Assert.IsFalse(Create().CanSetUpAi);
        }

        [TestMethod]
        public void WatchVideo_OpensTheWalkthrough()
        {
            Create().WatchVideoCommand.Execute(null);

            Assert.AreEqual("https://boringjob.work/placekit/guide/?utm_source=placekit&utm_medium=add-in&utm_content=welcome", _urls.Opened.Single().AbsoluteUri);
        }
    }
}
