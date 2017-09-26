﻿using System;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using IFunctionDeclaration = JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree.IFunctionDeclaration;
using IIdentifier = JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree.IIdentifier;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Daemon.Stages
{
    [DaemonStage(StagesBefore = new[] { typeof(GlobalFileStructureCollectorStage) },
        StagesAfter = new [] { typeof(CollectUsagesStage)} )]
    public class CgIdentifierHighlightingStage : CgDaemonStageBase
    {
        private readonly ILogger myLogger;

        public CgIdentifierHighlightingStage(ILogger logger)
        {
            myLogger = logger;
        }
        
        protected override IDaemonStageProcess CreateProcess(
            IDaemonProcess process, IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICgFile file)
        {
            return new IdentifierHighlightingProcess(myLogger, process, settings, file);
        }

        private class IdentifierHighlightingProcess : CgDaemonStageProcessBase
        {
            private readonly ILogger myLogger;
            
            public IdentifierHighlightingProcess(ILogger logger, IDaemonProcess daemonProcess, IContextBoundSettingsStore settingsStore, ICgFile file)
                : base(daemonProcess, settingsStore, file)
            {
                myLogger = logger;
            }

            public override void VisitFieldOperatorNode(IFieldOperator fieldOperatorParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.FIELD_IDENTIFIER, fieldOperatorParam.FieldNode.GetDocumentRange()));
                base.VisitFieldOperatorNode(fieldOperatorParam, context);
            }

            public override void VisitPostfixExpressionNode(IPostfixExpression postfixExpressionParam, IHighlightingConsumer context)
            {
                // TODO: fix
                if (postfixExpressionParam.OperatorNode.FirstOrDefault() is ICallOperator
                 && postfixExpressionParam.OperandNode is IIdentifier functionName)
                {
                    context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.FUNCTION_IDENTIFIER, functionName.GetDocumentRange()));
                }
                
                base.VisitPostfixExpressionNode(postfixExpressionParam, context);
            }

            public override void VisitSingleVariableDeclarationNode(ISingleVariableDeclaration singleVariableDeclarationParam,
                IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.TYPE_IDENTIFIER, singleVariableDeclarationParam.TypeNode.GetDocumentRange()));
                base.VisitSingleVariableDeclarationNode(singleVariableDeclarationParam, context);
            }

            public override void VisitVariableDeclarationNode(IVariableDeclaration variableDeclarationParam, IHighlightingConsumer context)
            {
                HighlightNameNodes(variableDeclarationParam, context, CgHighlightingAttributeIds.VARIABLE_IDENTIFIER);
                base.VisitVariableDeclarationNode(variableDeclarationParam, context);
            }

            public override void VisitBuiltInTypeNode(IBuiltInType builtInTypeParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.KEYWORD, builtInTypeParam.GetDocumentRange()));
                base.VisitBuiltInTypeNode(builtInTypeParam, context);
            }

            public override void VisitFieldDeclarationNode(IFieldDeclaration fieldDeclarationParam, IHighlightingConsumer context)
            {
                var variableDeclaration = fieldDeclarationParam.ContentNode;
                var typeNameRange = variableDeclaration?.FirstVariableNode?.TypeNode?.GetDocumentRange();
                if (typeNameRange != null)
                {
                    context.AddHighlighting(
                        new CgHighlighting(CgHighlightingAttributeIds.TYPE_IDENTIFIER, typeNameRange.Value));
                    HighlightNameNodes(variableDeclaration, context, CgHighlightingAttributeIds.FIELD_IDENTIFIER);
                }
                
                base.VisitFieldDeclarationNode(fieldDeclarationParam, context);
            }
            
            public override void VisitStructDeclarationNode(IStructDeclaration structDeclarationParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.TYPE_IDENTIFIER, structDeclarationParam.NameNode.GetDocumentRange()));
                base.VisitStructDeclarationNode(structDeclarationParam, context);
            }

            public override void VisitFunctionDeclarationNode(IFunctionDeclaration functionDeclarationParam, IHighlightingConsumer context)
            {
                var header = functionDeclarationParam.HeaderNode;
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.TYPE_IDENTIFIER,
                    header.TypeNode.GetDocumentRange()));

                try
                {
                    context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.FUNCTION_IDENTIFIER,
                        header.NameNode.GetDocumentRange()));
                }
                catch (InvalidCastException ex)
                {
                    myLogger.LogExceptionSilently(ex);
                }

                base.VisitFunctionDeclarationNode(functionDeclarationParam, context);
            }

            public override void VisitSemanticNode(ISemantic semanticParam, IHighlightingConsumer context)
            {
                context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.KEYWORD, semanticParam.GetDocumentRange())); // TODO: add as proper keywords maybe
                base.VisitSemanticNode(semanticParam, context);
            }

            public override void VisitCallOperatorNode(ICallOperator callOperatorParam, IHighlightingConsumer context)
            {
                var parent = callOperatorParam.Parent as IPostfixExpression;
                if (parent?.OperandNode is IIdentifier operand) // TODO: this is wrong if this is the constructor of user-declared type
                    context.AddHighlighting(new CgHighlighting(CgHighlightingAttributeIds.FUNCTION_IDENTIFIER, operand.GetDocumentRange()));
                
                base.VisitCallOperatorNode(callOperatorParam, context);
            }

            private void HighlightNameNodes(IVariableDeclaration variableDeclaration, IHighlightingConsumer context, string highlightingAttributeId)
            {
                foreach (var name in variableDeclaration.NameNodes)
                {
                    context.AddHighlighting(new CgHighlighting(highlightingAttributeId, name.GetDocumentRange()));
                }
            }
        }
    }
}