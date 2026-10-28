using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlaceKit.Tools;

namespace PlaceKit.Tests.Tools
{
    [TestClass]
    public class RelayCommandTests
    {
        [TestMethod]
        public void Execute_RunsTheAction()
        {
            int runs = 0;
            var command = new RelayCommand(() => runs++);

            command.Execute(null);

            Assert.AreEqual(1, runs);
            Assert.IsTrue(command.CanExecute(null));
        }

        [TestMethod]
        public void CanExecute_AsksThePredicate()
        {
            bool enabled = false;
            var command = new RelayCommand(() => { }, () => enabled);

            Assert.IsFalse(command.CanExecute(null));
            enabled = true;
            Assert.IsTrue(command.CanExecute(null));
        }

        [TestMethod]
        public void TypedCommand_PassesTheParameter()
        {
            string received = null;
            var command = new RelayCommand<string>(value => received = value, value => value != null);

            command.Execute("item");

            Assert.AreEqual("item", received);
            Assert.IsFalse(command.CanExecute(null));
            Assert.IsFalse(command.CanExecute(42), "a parameter of another type counts as null");
        }
    }
}
