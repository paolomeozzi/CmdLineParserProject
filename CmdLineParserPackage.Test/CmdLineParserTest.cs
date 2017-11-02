using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CmdLineParserPackage;
namespace CmdLineParserPackage.Test
{
    [TestClass]
    public class CmdLineParserTest
    {
        public class RouteArgs
        {
            public string HostName;
            public bool SuppressHostnameResolution = false;
            public int Timeout = 2000;
            public int MaxHops = 20;
        }

        [TestMethod]
        public void Success_WithOptionValuePrefix()
        {
            //string[] args = new string[] { "www.google.it", "-d", "-w", "1000", "-h", "17" };
            string[] args = new string[] { "www.google.it", "-d", "-w:1000", "-h:17" };
            var ra = new RouteArgs();
            var success = new CmdLineParser(args)
                .OptionFormat("-x:x")
                .OnArgument(a => ra.HostName = a)
                .OnOption("d", () => ra.SuppressHostnameResolution = true)
                .OnOption<int>("w", time => ra.Timeout = time)
                .OnOption<int>("h", mh => ra.MaxHops = mh)
                .Parse() == ParseResult.Success;

            Assert.IsTrue(success);
            Assert.AreEqual(true, ra.SuppressHostnameResolution);
            Assert.AreEqual(1000, ra.Timeout);
            Assert.AreEqual(17, ra.MaxHops);
        }

        [TestMethod]
        public void Success_WithoutOptionValuePrefix()
        {
            string[] args = new string[] { "www.google.it", "-d", "-w", "1000", "-h", "17" };
            var ra = new RouteArgs();
            var success = new CmdLineParser(args)
                .OptionFormat("-x x")
                .OnArgument(a => ra.HostName = a)
                .OnOption("d", () => ra.SuppressHostnameResolution = true)
                .OnOption<int>("w", time => ra.Timeout = time)
                .OnOption<int>("h", mh => ra.MaxHops = mh)
                .Parse() == ParseResult.Success; 

            Assert.IsTrue(success);
            Assert.AreEqual(true, ra.SuppressHostnameResolution);
            Assert.AreEqual(1000, ra.Timeout);
            Assert.AreEqual(17, ra.MaxHops);
        }

        [TestMethod]
        public void Success_WithOptionZeroValuePrefix()
        {
            string[] args = new string[] { "www.google.it", "-d", "-w1000" };
            var ra = new RouteArgs();
            var p = new CmdLineParser(args)
                //.OptionValuePrefix("")
                .OptionFormat("-xx")
                .OnArgument(a => ra.HostName = a)
                .OnOption("d", () => ra.SuppressHostnameResolution = true)
                .OnOption<int>("w", time => ra.Timeout = time)
                .OnOption<int>("h", mh => ra.MaxHops = mh);

            var success = p.Parse() == ParseResult.Success;

            Assert.IsTrue(success);
            Assert.AreEqual(1000, ra.Timeout);
        }

        [TestMethod]
        public void Success_WithAlternateOptionPrefix()
        {
            string[] args = new string[] { "www.google.it", "/d", "/w:1000" };
            var ra = new RouteArgs();
            var p = new CmdLineParser(args)
                .OptionFormat("/x:x")
                .OnArgument(a => ra.HostName = a)
                .OnOption("d", () => ra.SuppressHostnameResolution = true)
                .OnOption<int>("w", time => ra.Timeout = time)
                .OnOption<int>("h", mh => ra.MaxHops = mh);

            var success = p.Parse() == ParseResult.Success;

            Assert.IsTrue(success);
            Assert.AreEqual(1000, ra.Timeout);
        }

        [TestMethod]
        public void Error_UnknowOption()
        {
            string[] args = new string[] { "www.google.it", "-boh", "-w:a" };
            var ra = new RouteArgs();
            var p = new CmdLineParser(args)
                .OptionFormat("-x:x")
                .OnArgument(a => ra.HostName = a)
                .OnOption("d", () => ra.SuppressHostnameResolution = true)
                .OnOption<int>("w", time => ra.Timeout = time)
                .OnOption<int>("h", mh => ra.MaxHops = mh);

            var success = p.Parse() == ParseResult.Success;

            Assert.IsFalse(success);
            Assert.AreEqual("boh", p.Error.Arg.Name);
            Assert.AreEqual(ParseErrorType.UnknowOption, p.Error.ErrorType);
        }

        [TestMethod]
        public void Error_InvalidOptionValue()
        {
            string[] args = new string[] { "www.google.it", "-d", "-w:a" };
            var ra = new RouteArgs();
            var p = new CmdLineParser(args)
                .OptionFormat("-x:x")
                .OnArgument(a => ra.HostName = a)
                .OnOption("d", () => ra.SuppressHostnameResolution = true)
                .OnOption<int>("w", time => ra.Timeout = time)
                .OnOption<int>("h", mh => ra.MaxHops = mh);

            var success = p.Parse() == ParseResult.Success;

            Assert.IsFalse(success);
            Assert.AreEqual("w", p.Error.Arg.Name);
            Assert.AreEqual(ParseErrorType.InvalidValue, p.Error.ErrorType);
        }

        [TestMethod]
        public void Error_ArgumentRequired()
        {
            string[] args = new string[] { "-d", "-w:a" };
            var ra = new RouteArgs();
            var p = new CmdLineParser(args)
                .OptionFormat("-x:x")
                .OnArgument(a => ra.HostName = a, ArgumentPolicy.Once)
                .OnOption("d", () => ra.SuppressHostnameResolution = true)
                .OnOption<int>("w", time => ra.Timeout = time)
                .OnOption<int>("h", mh => ra.MaxHops = mh);

            var success = p.Parse() == ParseResult.Success;

            Assert.IsFalse(success);
            Assert.AreEqual(null, p.Error.Arg);
            Assert.AreEqual(ParseErrorType.ArgomentRequired, p.Error.ErrorType);
        }

        [TestMethod]
        public void Error_ArgumentOnce()
        {
            string[] args = new string[] { "uno", "due", "-d", "-w:a" };
            var ra = new RouteArgs();
            var p = new CmdLineParser(args)
                .OptionFormat("-x:x")
                .OnArgument(a => ra.HostName = a, ArgumentPolicy.Once)
                .OnOption("d", () => ra.SuppressHostnameResolution = true)
                .OnOption<int>("w", time => ra.Timeout = time)
                .OnOption<int>("h", mh => ra.MaxHops = mh);

            var success = p.Parse() == ParseResult.Success;

            Assert.IsFalse(success);
            Assert.AreEqual("due", p.Error.Arg.Text);
            Assert.AreEqual(ParseErrorType.MultipleArgumentNotAllowed, p.Error.ErrorType);
        }

    }
}
