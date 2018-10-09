using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoCore.DSASM;
using ProtoTest.TD;
using ProtoTestFx.TD;
namespace ProtoTest.Associative
{
    [TestFixture]
    class TypeSystemTests
    {
        public TestFrameWork thisTest = new TestFrameWork();
        [SetUp]
        public void Setup()
        {
        }


        [Test]
        public void ArrayConvTest()
        {
            String code =
                @"
            var mirror = thisTest.RunScriptSource(code);
            thisTest.Verify("a", new Object[] { 4 });
        }


        [Test]
        [Category("DSDefinedClass_Ported")]
        public void RedefConvTest()
        {
            String code =
                @"
            var mirror = thisTest.RunScriptSource(code);
            thisTest.Verify("v", 3);
        }


        [Test]
        public void RetArrayTest()
        {
            //DNL-1467221 Sprint 26 - Rev 3345 type conversion to array as return type does not get converted
            String code =
                @"
            var mirror = thisTest.RunScriptSource(code);
            thisTest.Verify("b2", new object[] { 2 });
        }

        [Test]
        public void RetArrayTest2()
        {
            //DNL-1467221 Sprint 26 - Rev 3345 type conversion to array as return type does not get converted
            String code =
                @"
            var mirror = thisTest.RunScriptSource(code);
            thisTest.Verify("b2", new object[] { 1.5 });
        }

        [Test]
        public void StatementArrayTest()
        {
            //DNL-1467221 Sprint 26 - Rev 3345 type conversion to array as return type does not get converted
            String code =
                @"
            var mirror = thisTest.RunScriptSource(code);
            thisTest.Verify("a", new object[] { 2 });
        }

        [Test]
        public void StatementArrayTest2()
        {
            //DNL-1467221 Sprint 26 - Rev 3345 type conversion to array as return type does not get converted
            String code =
                @"
            var mirror = thisTest.RunScriptSource(code);
            thisTest.Verify("a", new object[] { 1.5 });
        }

        [Test]
        public void Rep1()
        {
            String code =
                @"
            var mirror = thisTest.RunScriptSource(code);
            thisTest.Verify("a", 3.5);
        }

        [Test]
        public void Rep2()
        {
            String code =
                @"
            var mirror = thisTest.RunScriptSource(code);
            thisTest.Verify("a", 3.5);
        }

        [Test]
        public void Rep3()
        {
            String code =
                @"
            var mirror = thisTest.RunScriptSource(code);
            thisTest.Verify("a", new object[] { 3.5, 3.5 });
        }

        [Test]
        public void Rep4()
        {
            String code =
                @"
            var mirror = thisTest.RunScriptSource(code);
            thisTest.Verify("a", new object[] { new object[] { 3.5, 3.5 } });
        }

        [Test]
        public void Rep5()
        {
            //Assert.Fail("DNL-1467183 Sprint24: rev 3163 : replication on nested array is outputting extra brackets in some cases");
            String code =
                @"
            var mirror = thisTest.RunScriptSource(code);
            thisTest.Verify("a", new object[] { new object[] { 3.5, 3.5 }, 3.5 });
        }

        [Test]
        public void MinimalStringTest()
        {

            String code =
                @"a = ""Callsite is an angry bird"";
            var mirror = thisTest.RunScriptSource(code);
            StackValue sv = mirror.GetRawFirstValue("a");
            StackValue svb = mirror.GetRawFirstValue("b");
            thisTest.Verify("a", "Callsite is an angry bird");
        }

        [Test]
        public void SimpleUpCast()
        {
            String code =
                @"def foo(x:int[])
            var mirror = thisTest.RunScriptSource(code);
            thisTest.Verify("r", new Object[] { 1 });
        }

        [Test]
        public void TypedAssign()
        {
            String code =
                @"x : int = 2.3;";
            var mirror = thisTest.RunScriptSource(code);
            thisTest.Verify("x", 2);
        }

        [Test]
        public void TestVarUpcast()
        {
            string code =
                @"x : var[] = 3;";
            var mirror = thisTest.RunScriptSource(code);
            thisTest.Verify("x", new object[] { 3 });
        }

        [Test]
        public void TestVarDispatch()
        {
            string code =
                @"
            var mirror = thisTest.RunScriptSource(code);
            thisTest.Verify("y", new object[] { 3 });
            thisTest.Verify("z", new object[] { 3 });
            thisTest.Verify("z1", new object[] { new object[] { 3 } });
        }

        [Test]
        public void TestIntDispatch()
        {
            string code =
                @"
            var mirror = thisTest.RunScriptSource(code);
            thisTest.Verify("y", new object[] { 3 });
            thisTest.Verify("z", new object[] { 3 });
        }

        [Test]
        public void TestVarDispatchOnArrayStructure()
        {
            string code =
                @"
            string error = "1467326 - Sprint 27 - Rev 3905 when there is rank mismatch for function , array upagrades to 1 dimension higer than expected ";
            var mirror = thisTest.RunScriptSource(code, error);
            thisTest.Verify("y", new object[] { new object[] { 3 } });
            thisTest.Verify("z", new object[] { new object[] { 3 } });
        }

        [Test]
        public void TestVarDispatchOnArrayStructure2()
        {
            string code =
                @"
            string error = "1467326 - Sprint 27 - Rev 3905 when there is rank mismatch for function , array upagrades to 1 dimension higer than expected ";
            var mirror = thisTest.RunScriptSource(code, error);
            thisTest.Verify("y", new object[] { new object[] { new object[] { 3 } } });
            thisTest.Verify("z", new object[] { new object[] { new object[] { 3 } } });
            thisTest.Verify("z2", new object[] { new object[] { new object[] { 3 } } });
            thisTest.Verify("z3", new object[] { new object[] { new object[] { 3 } } });
        }

        [Test]
        public void TestIntDispatchOnArrayStructure()
        {
            string code =
                @"
            string error = "1467326 - Sprint 27 - Rev 3905 when there is rank mismatch for function , array upagrades to 1 dimension higer than expected ";
            var mirror = thisTest.RunScriptSource(code, error);
            thisTest.Verify("y", new object[] { new object[] { 3 } });
            thisTest.Verify("z", new object[] { new object[] { 3 } });
        }

        [Test]
        public void TestIntDispatchRetOnArrayStructure()
        {
            string code =
                @"
            string error = "1467326 - Sprint 27 - Rev 3905 when there is rank mismatch for function , array upagrades to 1 dimension higer than expected ";
            var mirror = thisTest.RunScriptSource(code, error);
            //thisTest.Verify(mirror, "y", 1);
            thisTest.Verify("z", 1);
            thisTest.Verify("z1", 1);
        }

        [Test]
        [Category("Failure")]
        public void TestIntSetOnArrayStructure()
        {
            // Tracked by http://adsk-oss.myjetbrains.com/youtrack/issue/MAGN-1668
            string code =
                @"
            string error = "MAGN-1668 Sprint 27 - Rev 3905 when there is rank mismatch for function , array upagrades to 1 dimension higer than expected";
            var mirror = thisTest.RunScriptSource(code, error);
            thisTest.Verify("x", new object[] { new object[] { 3 } });
            thisTest.Verify("y", new object[] { new object[] { 3 } });
            thisTest.Verify("z", new object[] { new object[] { 3 } });
            thisTest.Verify("z1", null);
        }

        [Test]
        public void TestIntDispatchOnArrayStructure2()
        {
            string code =
                @"
            string error = "1467326 - Sprint 27 - Rev 3905 when there is rank mismatch for function , array upagrades to 1 dimension higer than expected ";
            var mirror = thisTest.RunScriptSource(code, error);
            thisTest.Verify("y", new object[] { new object[] { new object[] { 3 } } });
            thisTest.Verify("z", new object[] { new object[] { new object[] { 3 } } });
            thisTest.Verify("z2", new object[] { new object[] { new object[] { 3 } } });
            thisTest.Verify("z3", new object[] { new object[] { new object[] { 3 } } });
        }


        [Test]
        public void TestVarReturnOnArrayStructure()
        {
            string code =
                @"
            var mirror = thisTest.RunScriptSource(code);
            thisTest.Verify("y", new object[] { 3 });
        }

        [Test]
        public void TestArbitraryRankArr()
        {
            string code =
                @"
            var mirror = thisTest.RunScriptSource(code);
            thisTest.Verify("a", new object[] { 3 });
            thisTest.Verify("b", 3);
            thisTest.Verify("y", new object[] { 3 });
            thisTest.Verify("z", new object[] { 3 });
        }

        [Test]
        public void TestAssignFailDueToRank()
        {
            string code =
                @"
            var mirror = thisTest.RunScriptSource(code);
            thisTest.Verify("a", null);
        }

    }
}
