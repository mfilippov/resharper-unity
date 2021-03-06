﻿using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Extentions;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.DotNetCore;
using JetBrains.ReSharper.UnitTestFramework.Elements;
using JetBrains.ReSharper.UnitTestFramework.Strategy;
using JetBrains.ReSharper.UnitTestProvider.nUnit;
using JetBrains.ReSharper.UnitTestProvider.nUnit.v30;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    [SolutionComponent]
    public class UnityNUnitServiceProvider : NUnitServiceProvider
    {
        private readonly UnityEditorProtocol myUnityEditorProtocol;
        private readonly RunViaUnityEditorStrategy myUnityEditorStrategy;
        private readonly RdUnityModel myRdUnityModel;
        
        public UnityNUnitServiceProvider(ISolution solution, IPsiModules psiModules, ISymbolCache symbolCache,
            IUnitTestElementIdFactory idFactory, IUnitTestElementManager elementManager, NUnitTestProvider provider,
            ISettingsStore settingsStore, ISettingsOptimization settingsOptimization, ISettingsCache settingsCache,
            UnitTestingCachingService cachingService, IDotNetCoreSdkResolver dotNetCoreSdkResolver,
            UnityEditorProtocol unityEditorProtocol,
            RunViaUnityEditorStrategy runViaUnityEditorStrategy,
            NUnitOutOfProcessUnitTestRunStrategy nUnitOutOfProcessUnitTestRunStrategy)
            : base(solution, psiModules, symbolCache, idFactory, elementManager, provider, settingsStore,
                settingsOptimization, settingsCache, cachingService, dotNetCoreSdkResolver, nUnitOutOfProcessUnitTestRunStrategy)
        {
            if (solution.GetData(ProjectModelExtensions.ProtocolSolutionKey) == null)
                return;
            
            myRdUnityModel = solution.GetProtocolSolution().GetRdUnityModel();

            myUnityEditorProtocol = unityEditorProtocol;
            myUnityEditorStrategy = runViaUnityEditorStrategy;
        }

        public override IUnitTestRunStrategy GetRunStrategy(IUnitTestElement element)
        {
            if (myUnityEditorProtocol.UnityModel.Value == null)
                return base.GetRunStrategy(element);

            if (!myRdUnityModel.UnitTestPreference.HasValue() || myRdUnityModel.UnitTestPreference.Value ==
                UnitTestLaunchPreference.NUnit)
            {
                return base.GetRunStrategy(element);
            }
            
            return myUnityEditorStrategy;
        }
    }
}