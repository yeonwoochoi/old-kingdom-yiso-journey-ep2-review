using Editor.Tests.Utils;

namespace Gameplay.Character.StateMachine.Actions.Tests {
    using NUnit.Framework;
    using Moq;
    using UnityEngine;
    using Core;
    using Core.Modules;
    using Data;
    
    public class YisoFSMActionTests {
        private Mock<IYisoCharacterContext> _mockContext;
        private YisoCharacterBlackboardModule _blackboardModule;
        
        private YisoBlackboardKeySO _minKey;
        private YisoBlackboardKeySO _maxKey;
        private YisoBlackboardKeySO _resultKey;

        [SetUp]
        public void Setup() {
            _mockContext = new Mock<IYisoCharacterContext>();
            
            _blackboardModule = new YisoCharacterBlackboardModule(_mockContext.Object);
            _mockContext.Setup(c => c.GetModule<YisoCharacterBlackboardModule>()).Returns(_blackboardModule);
            
            _minKey = ScriptableObject.CreateInstance<YisoBlackboardKeySO>();
            _maxKey = ScriptableObject.CreateInstance<YisoBlackboardKeySO>();
            _resultKey = ScriptableObject.CreateInstance<YisoBlackboardKeySO>();
        }

        [Test]
        public void SetRandomWaitTime__WhenExecuted__ShouldStoreValueBetweenMinAndMax() {
            var action = ScriptableObject.CreateInstance<YisoCharacterActionSetRandomWaitTimeSO>();
            
            TestUtils.SetPrivateField(action, "minTimeKey", _minKey);
            TestUtils.SetPrivateField(action, "maxTimeKey", _maxKey);
            TestUtils.SetPrivateField(action, "resultKey", _resultKey);
            
            _blackboardModule.SetFloat(_minKey, 2.0f);
            _blackboardModule.SetFloat(_maxKey, 5.0f);
            
            action.PerformAction(_mockContext.Object);
            
            var result = _blackboardModule.GetFloat(_resultKey);
            Debug.Log($"Generated Random Time: ${result}");
            
            Assert.GreaterOrEqual(result, 2.0f);
            Assert.LessOrEqual(result, 5.0f);
        }
    }
}