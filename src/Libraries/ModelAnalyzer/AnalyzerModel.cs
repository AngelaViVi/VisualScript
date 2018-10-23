﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dynamo.Utilities;
using ModelAnalyzer.Properties;
using ProtoCore.AST.AssociativeAST;
using System.Xml;
using DeepLearning.Visible;
using Dynamo.Graph;
using Dynamo.Graph.Nodes;
using Newtonsoft.Json;
using ModelAnalyzer;
using ProtoCore.AST;
using ProtoCore.SyntaxAnalysis;
using ProtoCore.SyntaxAnalysis.Associative;
using DynamoConversions;

/*
* 带客制化界面的元素的节点API
* 1.输出到bin/node中.
* 2.不需要在PathResolvers.cs中加载DLL,node目录中的dll是由NodeModelAssemblyLoader.cs加载的
* 3.与zerotouchlibrary的编写有较大的区别,不是通过函数的访问控制符来控制Node是否出现在list中的.
* 4.属性说明:
*       [NodeName("ExportToSAT")]Node的名字
*/
namespace ModelAnalyzerUI
{
    [NodeCategory("DeepLearning.Core")]
    [NodeName("ModelAgent")]
    //[InPortTypes("string")]
    [NodeDescription("ExportToSATDescripiton", typeof(Resources))]
    [NodeSearchTags("ExportWithUnitsSearchTags", typeof(Resources))]
    [IsDesignScriptCompatible]
    public class AnalyzerModel : NodeModel
    {
        private ObservableCollection<string> exportableNodeSource;
        private string selectedExportableNode;
        /// <summary>
        /// Combox数据源,填充可展开节点的名字(key)
        /// 此处存储实际数据
        /// </summary>
        public ObservableCollection<string> ExportableNodeSource
        {
            get
            {
                return exportableNodeSource;
            }
            set
            {
                exportableNodeSource = value;
                //this.OnNodeModified();
                RaisePropertyChanged("ExportableNodeSource");
            }
        }
        /// <summary>
        /// Combox当前选中项
        /// 此处存储实际数据
        /// </summary>
        public string SelectedExportableNode
        {
            get => selectedExportableNode;
            set
            {
                selectedExportableNode = value;
                //this.OnNodeModified();//会触发节点重绘
                RaisePropertyChanged("SelectedExportableNode");
            }
        }

        

        [JsonConstructor]
        private AnalyzerModel(IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) : base(inPorts, outPorts)
        {
            //TODO:内部变量的初始化
            ShouldDisplayPreviewCore = true;
            exportableNodeSource = new ObservableCollection<string>();
            selectedExportableNode = "";
        }

        public AnalyzerModel()
        {
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("Model_File", "模型文件")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("Label_File", "标签文件")));
            InPorts.Add(new PortModel(PortType.Input, this, new PortData("Input_File", "输入文件")));
            OutPorts.Add(new PortModel(PortType.Output, this, new PortData("Result", "输出")));

            ShouldDisplayPreviewCore = true;
            RegisterAllPorts();
        }
        /// <summary>
        /// 构建输出节点
        /// 这个函数的触发条件是输入节点发生了变化,即this.OnNodeModified();
        /// </summary>
        /// <param name="inputAstNodes"></param>
        /// <returns></returns>
        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {

            if (!InPorts[0].IsConnected || !InPorts[1].IsConnected || !InPorts[2].IsConnected)
            {
                //如果在没有连接输入节点的情况下连接了输出节点,应当有默认输出
                return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode()) };
            }


            var input1 = inputAstNodes[0];
            var input2 = inputAstNodes[1];
            var input3 = inputAstNodes[2];

            AssociativeNode node = null;

            node = AstFactory.BuildFunctionCall(
                        new Func<string, string, string, string>(DeepLearning.Example.Predict),
                        new List<AssociativeNode> { input1, input2, input3 });

            return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), node) };
        }

        #region 重载:序列化和解序列化方法
        protected override void SerializeCore(XmlElement element, SaveContext context)
        {
            base.SerializeCore(element, context); // Base implementation must be called.

            var helper = new XmlElementHelper(element);
            helper.SetAttribute("UINodeExample",0);//把slider的值存起来
        }

        protected override void DeserializeCore(XmlElement element, SaveContext context)
        {
            base.DeserializeCore(element, context); //Base implementation must be called.
            var helper = new XmlElementHelper(element);
            var exportedUnit = helper.ReadString("UINodeExample");

            //valueofslider = int.Parse(exportedUnit) is int ? int.Parse(exportedUnit) : 0;
        }

        #endregion
    }
}

