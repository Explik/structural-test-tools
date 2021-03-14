﻿using Lecture_9_Solutions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using TestTools.Structure;
using TestTools.Unit;
using static TestTools.Unit.TestExpression;
using static Lecture_9_Tests.TestHelper;
using static TestTools.Helpers.StructureHelper;
using System.Collections.Specialized;

namespace Lecture_9_Tests
{
    [TestClass]
    public class Exercise_4_Tests
    {
        #region Exercise 4A
        [TestMethod("a. ObservableCollection<T> implements ICollection"), TestCategory("Exercise 4A")]
        public void ObservableCollectionImplementsICollection()
        {
            // TestTools Code
            StructureTest test = Factory.CreateStructureTest();
            test.AssertClass<ObservableCollection<int>>(new TypeIsSubclassOfVerifier(typeof(ICollection<int>)));
            test.Execute();
        }
        #endregion

        #region Exercise 4B
        [TestMethod("a. ObservableCollection<T> implements INotifyCollectionChanged"), TestCategory("Exercise 4B")]
        public void ObservableCollectionImplementsINotifyCollectionChanged()
        {
            // TestTools Code
            StructureTest test = Factory.CreateStructureTest();
            test.AssertClass<ObservableCollection<int>>(new TypeIsSubclassOfVerifier(typeof(INotifyCollectionChanged)));
            test.Execute();
        }

        [TestMethod("b. ObservableCollection<T>.Add(T elem) emits CollectionChanged event"), TestCategory("Exercise 4B")]
        public void ObservableCollectionAddEmitsCollectionChangedEvents()
        {
            ObservableCollection<int> collection = new ObservableCollection<int>();
            DelegateAssert.IsInvoked<NotifyCollectionChangedEventHandler>(handler => collection.CollectionChanged += handler);

            collection.Add(5);

            DelegateAssert.Verify();

            // TestTools Code
            UnitTest test = Factory.CreateTest();
            TestVariable<ObservableCollection<int>> _collection = test.CreateVariable<ObservableCollection<int>>();
            test.Arrange(_collection, Expr(() => new ObservableCollection<int>()));
            test.DelegateAssert.IsInvoked(Lambda<NotifyCollectionChangedEventHandler>(handler => Expr(_collection, c => c.AddCollectionChanged(handler))));
            test.Act(Expr(_collection, c => c.Add(5)));
            test.Execute();
        }

        [TestMethod("c. ObservableCollection<T>.Clear() emits CollectionChanged event"), TestCategory("Exercise 4B")]
        public void ObservableCollectionClearEmitsCollectionChangedEvent()
        {
            ObservableCollection<int> collection = new ObservableCollection<int>() { 1 };
            DelegateAssert.IsInvoked<NotifyCollectionChangedEventHandler>(handler => collection.CollectionChanged += handler);

            collection.Clear();

            DelegateAssert.Verify();

            // TestTools Code
            UnitTest test = Factory.CreateTest();
            TestVariable<ObservableCollection<int>> _collection = test.CreateVariable<ObservableCollection<int>>();
            test.Arrange(_collection, Expr(() => new ObservableCollection<int>() { 1 }));
            test.DelegateAssert.IsInvoked(Lambda<NotifyCollectionChangedEventHandler>(handler => Expr(_collection, c => c.AddCollectionChanged(handler))));
            test.Act(Expr(_collection, c => c.Clear()));// TestTools Code
            test.Execute();
        }

        [TestMethod("d. ObservableCollection<T>.Remove(T elem) emits CollectionChanged event"), TestCategory("Exercise 4B")]
        public void ObservableCollectionRemoveEmitsCollectionChangedEvent()
        {
            ObservableCollection<int> collection = new ObservableCollection<int>() { 1 };
            DelegateAssert.IsInvoked<NotifyCollectionChangedEventHandler>(handler => collection.CollectionChanged += handler);

            collection.Remove(1);

            DelegateAssert.Verify();

            // TestTools Code
            UnitTest test = Factory.CreateTest();
            TestVariable<ObservableCollection<int>> _collection = test.CreateVariable<ObservableCollection<int>>();
            test.Arrange(_collection, Expr(() => new ObservableCollection<int>() { 1 }));
            test.DelegateAssert.IsInvoked(Lambda<NotifyCollectionChangedEventHandler>(handler => Expr(_collection, c => c.AddCollectionChanged(handler))));
            test.Act(Expr(_collection, c => c.Remove(1)));
            test.Execute();
        }
        #endregion
    }
}
