using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace MacroEngine.Core
{
    public class ReportGenerator
    {
        private string _scenarioName;
        private string _reportPath;
        private List<TestCase> _testCases = new List<TestCase>();
        private int _failures = 0;
        private int _errors = 0;
        private double _totalTime = 0.0;

        private class TestCase
        {
            public string Name { get; set; }
            public double Time { get; set; }
            public bool Success { get; set; }
        }

        public ReportGenerator(string scenarioName, string reportPath)
        {
            _scenarioName = scenarioName;
            _reportPath = reportPath;
        }

        public void AddResult(string stepType, double elapsedSeconds, bool success)
        {
            _testCases.Add(new TestCase { Name = stepType, Time = elapsedSeconds, Success = success });
            _totalTime += elapsedSeconds;
            if (!success) _failures++;
        }

        public void Generate()
        {
            try
            {
                if (!Directory.Exists(_reportPath))
                {
                    Directory.CreateDirectory(_reportPath);
                }

                XElement suite = new XElement("testsuite",
                    new XAttribute("name", _scenarioName),
                    new XAttribute("tests", _testCases.Count),
                    new XAttribute("failures", _failures),
                    new XAttribute("errors", _errors),
                    new XAttribute("skipped", 0),
                    new XAttribute("time", Math.Round(_totalTime, 2)));

                foreach (var tc in _testCases)
                {
                    var tcNode = new XElement("testcase",
                        new XAttribute("name", tc.Name),
                        new XAttribute("time", Math.Round(tc.Time, 2)));
                    if (!tc.Success)
                    {
                        tcNode.Add(new XElement("failure", new XAttribute("message", "Step failed verification.")));
                    }
                    suite.Add(tcNode);
                }

                XDocument xmlDoc = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), suite);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string file = Path.Combine(_reportPath, $"{_scenarioName}_{timestamp}.xml");
                xmlDoc.Save(file);
                
                string jsonFile = Path.Combine(_reportPath, $"{_scenarioName}_{timestamp}.json");
                File.WriteAllText(jsonFile, $"{{ \"scenario\": \"{_scenarioName}\", \"status\": \"{( _failures == 0 ? "PASSED" : "FAILED" )}\", \"duration\": {_totalTime} }}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to save report: {ex.Message}");
            }
        }
    }
}
