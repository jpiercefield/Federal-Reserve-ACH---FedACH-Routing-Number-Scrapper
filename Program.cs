using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using OpenQA.Selenium.Support.UI;

namespace ACHSrapper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Launch headless Chrome browser
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArguments("--headless");
            using var driver = new ChromeDriver(chromeOptions);

            // Navigate to the ACH search page
            driver.Navigate().GoToUrl("https://www.frbservices.org/EPaymentsDirectory/achResults.html?bank=&aba=01&state=+&city=+&submitButton=Search&referredBy=searchAchPage");

            // Accept the terms and conditions
            var agreeButton = driver.FindElement(By.Id("agree_terms_use"));
            agreeButton.Click();

            // Loop through the ABA numbers and extract the data
            var baseUrl = "https://www.frbservices.org/EPaymentsDirectory/achResults.html?bank=&aba={0}&state=+&city=+&submitButton=Search&referredBy=searchAchPage";
            var bankList = new List<ACHInfo>();
            for (int i = 1; i <= 999; i++)
            {
                Console.WriteLine("Iteration " + i.ToString() + " out of 999.");
                var aba = i.ToString().PadLeft(3, '0');
                var url = string.Format(baseUrl, aba);

                // Navigate to the ABA search results page
                driver.Navigate().GoToUrl(url);
                bool error = false;
                try
                {
                    var errorElement = driver.FindElement(By.Id("error_msg")); //When 499 or more results pull up for a 3 digit combo, this message will appear, need to further specify routing number
                    if(errorElement != null)
                    {
                        error = true;
                    }
                } catch (Exception) {
                    //Ignore
                }

                if (error)
                {
                    for(int k = 0; k < 10; k++)
                    {
                        var subAba = aba + k.ToString(); //Loop ABA + sub value
                        var subUrl = string.Format(baseUrl, subAba);
                        driver.Navigate().GoToUrl(subUrl);
                        IWebElement dropdown = driver.FindElement(By.Name("results_table_length"));
                        SelectElement select = new(dropdown);
                        select.SelectByValue("100");
                        bool finished = false;
                        while (!finished)
                        {
                            var table = driver.FindElement(By.Id("results_table"));
                            var rows = table.FindElements(By.TagName("tr"));
                            foreach (var row in rows)
                            {
                                var columns = row.FindElements(By.TagName("td"));
                                if (columns.Count == 5)
                                {
                                    string routingNumber = columns[1].Text.Trim().Replace("-", "");
                                    var bank = new ACHInfo
                                    {
                                        RoutingNumber = routingNumber.Length == 8 ? "0" + routingNumber : routingNumber, //Set leading 0 if needed
                                        Name = columns[2].Text.Trim().Replace(",", " "),
                                        City = columns[3].Text.Trim().Replace(",", " "),
                                        State = columns[4].Text.Trim()
                                    };
                                    bankList.Add(bank);
                                }
                            }

                            var nextButton = driver.FindElement(By.CssSelector("a#results_table_next"));
                            if (nextButton != null)
                            {
                                bool isDisabled = nextButton.GetAttribute("class").Contains("disabled");
                                if (!isDisabled)
                                {
                                    nextButton.Click();
                                } else {
                                    finished = true;
                                }
                            } else {
                                finished = true;
                            }
                        }
                    }
                } else {
                    IWebElement dropdown = driver.FindElement(By.Name("results_table_length"));
                    SelectElement select = new(dropdown);
                    select.SelectByValue("100");
                    bool finished = false;
                    while (!finished)
                    {
                        var table = driver.FindElement(By.Id("results_table"));
                        var rows = table.FindElements(By.TagName("tr"));

                        foreach (var row in rows)
                        {
                            var columns = row.FindElements(By.TagName("td"));
                            if (columns.Count == 5)
                            {
                                string routingNumber = columns[1].Text.Trim().Replace("-", "");
                                var bank = new ACHInfo
                                {
                                    RoutingNumber = routingNumber.Length == 8 ? "0" + routingNumber : routingNumber, //Set leading 0 if needed
                                    Name = columns[2].Text.Trim().Replace(",", " "),
                                    City = columns[3].Text.Trim().Replace(",", " "),
                                    State = columns[4].Text.Trim()
                                };
                                bankList.Add(bank);
                            }
                        }

                        var nextButton = driver.FindElement(By.CssSelector("a#results_table_next"));
                        if (nextButton != null)
                        {
                            bool isDisabled = nextButton.GetAttribute("class").Contains("disabled");
                            if (!isDisabled)
                            {
                                nextButton.Click();
                            } else {
                                finished = true;
                            }
                        } else {
                            finished = true;
                        }
                    }
                }
                
            }

            using StreamWriter writer = new("ACHRoutingNumbers.csv");
            writer.WriteLine("Routing Number,Name,City,State");
            foreach (var bank in bankList)
            {
                writer.WriteLine($"{bank.RoutingNumber},{bank.Name},{bank.City},{bank.State}");
            }
        }
    }
}