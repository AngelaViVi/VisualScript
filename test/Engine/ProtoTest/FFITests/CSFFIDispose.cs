using System;

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]

        [Test]
        [Category("DSDefinedClass_Ported")]


        [Test]
        [Ignore][Category("DSDefinedClass_Ignored_DSDefinedClassSemantics")]
        {
            string code=
@"
            thisTest.RunScriptSource(code);
            thisTest.Verify("count1", 0);
            thisTest.Verify("count2", 2);
        }

        [Test]
        public void DisposeMultipleDispoableObject()
        {
            string code =
@"
";
            thisTest.RunScriptSource(code);
            thisTest.Verify("count1", 0);
            thisTest.Verify("count2", 0);

            thisTest.Verify("d1", 2);
            thisTest.Verify("d2", 3);