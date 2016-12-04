using System;
using System.Threading;
using Miruken.Concurrency;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Miruken.Infrastructure;

namespace Miruken.Tests.Concurrency
{
    /// <summary>
    /// Summary description for LinqToPromisesTests
    /// </summary>
    [TestClass]
    public class LinqToPromisesTests
    {
        [TestMethod]
        public void Should_Accept_Linq_Fulfilled_Synchronous_Promise()
        {
            var called  = false;
            var promise =
                (from x in Promise.Resolved("Hello")
                 select x)
                .Then((result, s) => {
                    Assert.AreEqual("Hello", result);
                    called = true;
                });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
                Assert.IsTrue(promise.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");            
        }

        [TestMethod]
        public void Should_Accept_Linq_Rejected_Synchronous_Promise()
        {
            var called  = false;
            var promise =
                (from x in Promise<int>.Rejected(new InvalidOperationException("Out of order"))
                 select x)
                .Catch((InvalidOperationException ex, bool s) => {
                    Assert.AreEqual("Out of order", ex.Message);
                    called = true;
                });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
                Assert.IsTrue(promise.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Accept_Linq_Fulfilled_Asynchronous_Promise()
        {
            var called  = false;
            var promise =
                (from x in new Promise<object>((resolve, reject) =>
                    ThreadPool.QueueUserWorkItem(_ => resolve("Hello", false)))
                 select x)
                .Then((result, s) => {
                    Assert.AreEqual("Hello", result);
                    called = true;
                });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
                Assert.IsFalse(promise.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Accept_Linq_Rejected_Asynchronous_Promise()
        {
            var called  = false;
            var promise =
                (from x in new Promise<object>((resolve, reject) =>
                    ThreadPool.QueueUserWorkItem(_ =>
                        reject(new ArgumentException("Empty"), false)))
                 select x)
                .Catch((ArgumentException ex, bool s) => {
                    Assert.AreEqual("Empty", ex.Message);
                    called = true;
                });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
                Assert.IsFalse(promise.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Chain_Linq_Fulfilled_Asynchronous_Promises()
        {
            var called  = false;
            var promise =
                (from zipCode in GetZipCode("Rockwall, TX")
                 from weather in GetWeather(zipCode)
                 select weather)
                .Then((result, s) => {
                    Assert.AreEqual("Today's weather for 75032 is Rainy, high of 75", result);
                    called = true;
                });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
                Assert.IsFalse(promise.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Chain_Linq_Rejected_Asynchronous_Promises()
        {
            var called  = false;
            var promise =
                (from zipCode in GetZipCode("Valley Stream, NY")
                 from weather in GetWeather(zipCode)
                 select weather)
                .Catch((ArgumentException ex, bool s) => {
                    Assert.AreEqual("Unknown Valley Stream, NY", ex.Message);
                    called = true;
                });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
                Assert.IsFalse(promise.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Cancel_Linq_Fulfilled_Asynchronous_Promise()
        {
            var called  = false;
            var cancel  = false;
            var promise =
                (from zipCode in GetZipCode("Rockwall, TX")
                 where zipCode != 75032
                 from weather in GetWeather(zipCode)
                 select weather)
                .Then((result, s) => { called = true; });
            promise.Cancelled(ex => cancel = true);
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.AreEqual(PromiseState.Cancelled, promise.State);
                Assert.IsFalse(called);
                Assert.IsTrue(cancel);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Join_Linq_Fulfilled_Asynchronous_Promises()
        {
            var called  = false;
            var promise =
                (from person   in GetPerson("123456")
                 join employee in GetEmployee("123456") on person.SSN equals employee.Id
                 select new Employment { Person = person, Employee = employee })
                .Then((result, s) => {
                    Assert.AreEqual("123456",  result.Person.SSN);
                    Assert.AreEqual("John",    result.Person.FirstName);
                    Assert.AreEqual("Smith",   result.Person.LastName);
                    Assert.AreEqual("123456",  result.Employee.Id);
                    Assert.AreEqual("Manager", result.Employee.Title);
                    Assert.AreEqual(50000M,    result.Employee.Salaray);
                    called = true;
                });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.IsTrue(called);
                Assert.IsFalse(promise.CompletedSynchronously);
            }
            else
                Assert.Fail("Operation timed out");
        }

        [TestMethod]
        public void Should_Cancel_Unmatched_Join_Linq_Asynchronous_Promises()
        {
            var called  = false;
            var promise =
                (from person   in GetPerson("123456")
                 join employee in GetEmployee("123789") on person.SSN equals employee.Id
                 select new Employment { Person = person, Employee = employee })
                .Then((result, s) => { called = true; });
            if (promise.AsyncWaitHandle.WaitOne(5.Sec()))
            {
                Assert.AreEqual(PromiseState.Cancelled, promise.State);
                Assert.IsFalse(called);
            }
            else
                Assert.Fail("Operation timed out");
        }

        private static Promise<int> GetZipCode(string cityState)
        {
            return new Promise<int>((resolve, reject) =>
                ThreadPool.QueueUserWorkItem(_ => {
                    if (cityState == "Rockwall, TX")
                        resolve(75032, false);
                    else
                        reject(new ArgumentException("Unknown " + cityState), false);
                }));
        }

        private static Promise<string> GetWeather(int zipCode)
        {
            return Promise.Delay(100.Millis()).Then((r, s) =>
                $"Today's weather for {zipCode} is Rainy, high of 75");
        }

        private static Promise<Person> GetPerson(string ssn)
        {
            return new Promise<Person>((resolve, reject) =>
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    if (ssn.StartsWith("123"))
                        resolve(new Person
                        {
                            FirstName = "John",
                            LastName  = "Smith",
                            SSN       = ssn
                        }, false);
                    else
                        reject(new ArgumentException("Unknown " + ssn), false);
                }));
        }

        private static Promise<Employee> GetEmployee(string id)
        {
            return new Promise<Employee>((resolve, reject) =>
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    if (id.StartsWith("123"))
                        resolve(new Employee
                        {
                            Id      = id,
                            Title   = "Manager",
                            Salaray = 50000M
                        }, false);
                    else
                        reject(new ArgumentException("Unknown " + id), false);
                }));
        }

        private class Person
        {
            public string FirstName { get; set; }
            public string LastName  { get; set; }
            public string SSN       { get; set; }
        }

        private class Employee
        {
            public string  Id      { get; set; }
            public string  Title   { get; set; }
            public decimal Salaray { get; set; }
        }

        private class Employment
        {
            public Person   Person   { get; set; }
            public Employee Employee { get; set; }
        }
    }
}
