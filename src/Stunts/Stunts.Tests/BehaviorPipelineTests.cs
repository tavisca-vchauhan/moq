﻿using System;
using System.Reflection;
using Xunit;

namespace Stunts.Tests
{
    public class BehaviorPipelineTests
    {
        [Fact]
        public void WhenInvokingPipeline_ThenInvokesAllBehaviorsAndTarget()
        {
            var firstCalled = false;
            var secondCalled = false;
            var targetCalled = false;

            var pipeline = new BehaviorPipeline(
                new ExecuteDelegate((m, n) => { firstCalled = true; return n().Invoke(m, n); }),
                new ExecuteDelegate((m, n) => { secondCalled = true; return n().Invoke(m, n); }));

            Action a = WhenInvokingPipeline_ThenInvokesAllBehaviorsAndTarget;
            
            pipeline.Invoke(new MethodInvocation(this, a.GetMethodInfo()),
                new ExecuteDelegate((m, n) => { targetCalled = true; return m.CreateValueReturn(null); }));

            Assert.True(firstCalled);
            Assert.True(secondCalled);
            Assert.True(targetCalled);
        }

        [Fact]
        public void WhenInvokingPipeline_ThenSkipsNonApplicableBehaviors()
        {
            var firstCalled = false;
            var secondCalled = false;
            var targetCalled = false;

            var pipeline = new BehaviorPipeline(
                StuntBehavior.Create((m, n) => { firstCalled = true; return n().Invoke(m, n); }),
                StuntBehavior.Create((m, n) => { secondCalled = true; return n().Invoke(m, n); }, m => false));

            Action a = WhenInvokingPipeline_ThenInvokesAllBehaviorsAndTarget;

            pipeline.Invoke(new MethodInvocation(this, a.GetMethodInfo()),
                new ExecuteDelegate((m, n) => { targetCalled = true; return m.CreateValueReturn(null); }));

            Assert.True(firstCalled);
            Assert.False(secondCalled);
            Assert.True(targetCalled);
        }

        [Fact]
        public void WhenInvokingPipeline_ThenBehaviorCanShortcircuitInvocation()
        {
            var firstCalled = false;
            var secondCalled = false;
            var targetCalled = false;

            var pipeline = new BehaviorPipeline(
                new ExecuteDelegate((m, n) => { firstCalled = true; return m.CreateValueReturn(null); }),
                new ExecuteDelegate((m, n) => { secondCalled = true; return n().Invoke(m, n); }));

            Action a = WhenInvokingPipeline_ThenBehaviorCanShortcircuitInvocation;

            pipeline.Invoke(new MethodInvocation(this, a.GetMethodInfo()),
                new ExecuteDelegate((m, n) => { targetCalled = true; return m.CreateValueReturn(null); }));

            Assert.True(firstCalled);
            Assert.False(secondCalled);
            Assert.False(targetCalled);
        }

        [Fact]
        public void WhenInvokingPipeline_ThenBehaviorsCanPassDataWithContext()
        {
            var expected = Guid.NewGuid();
            var actual = Guid.Empty;

            var pipeline = new BehaviorPipeline(
                new ExecuteDelegate((m, n) => { m.Context["guid"] = expected; return n().Invoke(m, n); }),
                new ExecuteDelegate((m, n) => { actual = (Guid)m.Context["guid"]; return n().Invoke(m, n); }));

            Action a = WhenInvokingPipeline_ThenBehaviorsCanPassDataWithContext;

            var result = pipeline.Invoke(new MethodInvocation(this, a.GetMethodInfo()),
                new ExecuteDelegate((m, n) => m.CreateValueReturn(null)));

            Assert.Equal(expected, actual);
            Assert.True(result.Context.ContainsKey("guid"));
            Assert.Equal(expected, result.Context["guid"]);
        }

        [Fact]
        public void WhenInvokingPipeline_ThenBehaviorsCanReturnValue()
        {
            var expected = new object();

            var pipeline = new BehaviorPipeline(
                new ExecuteDelegate((m, n) => m.CreateValueReturn(expected)));

            Func<object> a = NonVoidMethod;

            var result = pipeline.Invoke(new MethodInvocation(this, a.GetMethodInfo()),
                new ExecuteDelegate((m, n) => throw new NotImplementedException()));

            Assert.Equal(expected, result.ReturnValue);
        }

        [Fact]
        public void WhenInvokingPipeline_ThenBehaviorsCanReturnValueWithArg()
        {
            var expected = new object();

            var pipeline = new BehaviorPipeline(
                new ExecuteDelegate((m, n) => m.CreateValueReturn(expected, new object())));

            Func<object, object> a = NonVoidMethodWithArg;

            var result = pipeline.Invoke(new MethodInvocation(this, a.GetMethodInfo(), expected),
                new ExecuteDelegate((m, n) => throw new NotImplementedException()));

            Assert.Equal(expected, result.ReturnValue);
        }

        [Fact]
        public void WhenInvokingPipeline_ThenBehaviorsCanReturnValueWithRef()
        {
            var expected = new object();
            var output = new object();

            var pipeline = new BehaviorPipeline(
                new ExecuteDelegate((m, n) => m.CreateValueReturn(expected, new object(), output)));

            NonVoidMethodWithArgRefDelegate a = NonVoidMethodWithArgRef;

            var result = pipeline.Invoke(new MethodInvocation(this, a.GetMethodInfo(), expected, output),
                new ExecuteDelegate((m, n) => throw new NotImplementedException()));

            Assert.Equal(expected, result.ReturnValue);
            Assert.Equal(output, result.Outputs[0]);
        }

        [Fact]
        public void WhenInvokingPipeline_ThenBehaviorsCanReturnValueWithOut()
        {
            var expected = new object();
            var output = new object();

            var pipeline = new BehaviorPipeline(
                new ExecuteDelegate((m, n) => m.CreateValueReturn(expected, new object(), output)));

            NonVoidMethodWithArgOutDelegate a = NonVoidMethodWithArgOut;

            var result = pipeline.Invoke(new MethodInvocation(this, a.GetMethodInfo(), expected, output),
                new ExecuteDelegate((m, n) => throw new NotImplementedException()));

            Assert.Equal(expected, result.ReturnValue);
            Assert.Equal(output, result.Outputs[0]);
        }

        [Fact]
        public void WhenInvokingPipeline_ThenBehaviorsCanReturnValueWithRefOut()
        {
            var expected = new object();
            var output = new object();
            var byref = new object();

            var pipeline = new BehaviorPipeline(
                new ExecuteDelegate((m, n) => m.CreateValueReturn(expected, new object(), byref, output)));

            NonVoidMethodWithArgRefOutDelegate a = NonVoidMethodWithArgRefOut;

            var result = pipeline.Invoke(new MethodInvocation(this, a.GetMethodInfo(), expected, byref, output),
                new ExecuteDelegate((m, n) => throw new NotImplementedException()));

            Assert.Equal(expected, result.ReturnValue);
            Assert.Equal(byref, result.Outputs[0]);
            Assert.Equal(output, result.Outputs[1]);
        }

        delegate object NonVoidMethodWithArgRefDelegate(object arg1, ref object arg2);
        delegate object NonVoidMethodWithArgOutDelegate(object arg1, out object arg2);
        delegate object NonVoidMethodWithArgRefOutDelegate(object arg1, ref object arg2, out object arg3);

        object NonVoidMethod() => null;
        object NonVoidMethodWithArg(object arg) => null;
        object NonVoidMethodWithArgRef(object arg1, ref object arg2) => null;
        object NonVoidMethodWithArgOut(object arg1, out object arg2) { arg2 = new object (); return null; }
        object NonVoidMethodWithArgRefOut(object arg1, ref object arg2, out object arg3) { arg3 = new object(); return null; }
    }
}
