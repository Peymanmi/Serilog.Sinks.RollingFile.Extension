namespace Serilog.Sinks.RollingFile.AcceptanceTest.Steps
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using NUnit.Framework;

    using TechTalk.SpecFlow;

    [Binding]
    public sealed class Logger
    {
        // For additional details on SpecFlow step definitions see http://go.specflow.org/doc-stepdef

        [Given("I have entered (.*) into the logger")]
        public void GivenIHaveEnteredSomethingIntoTheCalculator(int number)
        {
            var eventList = new List<Exception>();
            for (var eventCnt = 0; eventCnt < number; eventCnt++)
                eventList.Add(new Exception(string.Format("Exception No:{0}", eventCnt)));

            ScenarioContext.Current.Add("Events", eventList);
        }

        [Then("Should log (.*) events file in log folder")]
        public void ThenTheResultShouldBe(int number)
        {
            //TODO: implement assert (verification) logic
            var logPath = @"C:\temp\logger\";
            var files = Directory.GetFiles(logPath);
            Assert.GreaterOrEqual(files.Count(), number);
        }

        [When("Logger log all events")]
        public void WhenIPressAdd()
        {
            //TODO: implement act (action) logic
            var eventList = ScenarioContext.Current.Get<List<Exception>>("Events");

            eventList.ForEach(
                exp =>
                    {
                        Log.Error(exp, "Logging new exception");
                        Thread.Sleep(5);
                    });
        }
    }
}