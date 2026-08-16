using System.Collections.Generic;
using System.Reflection;
using MissNibiru.Waves.Editor;
using MissNibiru.Waves.Layouts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MissNibiru.Waves.Tests
{
    public sealed class WaveLayoutEditorTests
    {
        private readonly List<Object> _createdObjects =
            new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object createdObject in _createdObjects)
            {
                if (createdObject != null)
                    Object.DestroyImmediate(createdObject);
            }

            _createdObjects.Clear();
        }

        [Test]
        public void BuilderWindow_OpensSuccessfully()
        {
            WaveLayoutBuilderWindow.Open();

            WaveLayoutBuilderWindow window =
                EditorWindow.GetWindow<
                    WaveLayoutBuilderWindow>();

            Assert.IsNotNull(window);
            window.Close();
        }

        [Test]
        public void Validator_RequiresWorldOrigin()
        {
            WaveLayoutData layout = Layout();

            List<WaveLayoutValidationIssue> issues =
                WaveLayoutValidator.Validate(layout, null, true);

            Assert.IsTrue(
                issues.Exists(
                    issue => issue.Severity ==
                             WaveLayoutValidationSeverity.Error &&
                             issue.Message.Contains("origin")));
        }

        [Test]
        public void Validator_DetectsSimultaneousOverlap()
        {
            WaveLayoutData layout = Layout();
            SpawnableDefinition spawnable = Spawnable();

            layout.Waves[0].Placements.Add(
                Placement(spawnable, new Vector2Int(2, 2)));
            layout.Waves[0].Placements.Add(
                Placement(spawnable, new Vector2Int(2, 2)));

            List<WaveLayoutValidationIssue> issues =
                WaveLayoutValidator.Validate(layout, null, false);

            Assert.IsTrue(
                issues.Exists(
                    issue => issue.Severity ==
                             WaveLayoutValidationSeverity.Error &&
                             issue.Message.Contains("overlap")));
        }

        [Test]
        public void Validator_AllowsSameCellAtDifferentTimes()
        {
            WaveLayoutData layout = Layout();
            SpawnableDefinition spawnable = Spawnable();

            WaveLayoutPlacement first =
                Placement(spawnable, new Vector2Int(2, 2));
            WaveLayoutPlacement second =
                Placement(spawnable, new Vector2Int(2, 2));
            second.SpawnDelay = 1f;

            layout.Waves[0].Placements.Add(first);
            layout.Waves[0].Placements.Add(second);

            List<WaveLayoutValidationIssue> issues =
                WaveLayoutValidator.Validate(layout, null, false);

            Assert.IsFalse(
                issues.Exists(
                    issue => issue.Message.Contains("overlap")));
        }

        [Test]
        public void Validator_DetectsOutOfBoundsFootprint()
        {
            WaveLayoutData layout = Layout();
            SpawnableDefinition spawnable = Spawnable(
                new Vector2Int(2, 2));

            layout.Waves[0].Placements.Add(
                Placement(spawnable, new Vector2Int(19, 11)));

            List<WaveLayoutValidationIssue> issues =
                WaveLayoutValidator.Validate(layout, null, false);

            Assert.IsTrue(
                issues.Exists(
                    issue => issue.Severity ==
                             WaveLayoutValidationSeverity.Error &&
                             issue.Message.Contains("bounds")));
        }

        [Test]
        public void Validator_DetectsInvalidRawFootprint()
        {
            WaveLayoutData layout = Layout();
            SpawnableDefinition spawnable = Spawnable();

            typeof(SpawnableDefinition)
                .GetField(
                    "gridFootprint",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic)
                ?.SetValue(spawnable, Vector2Int.zero);

            layout.Waves[0].Placements.Add(
                Placement(spawnable, Vector2Int.zero));

            List<WaveLayoutValidationIssue> issues =
                WaveLayoutValidator.Validate(layout, null, false);

            Assert.IsTrue(
                issues.Exists(
                    issue => issue.Message.Contains("footprint")));
        }

        private WaveLayoutData Layout()
        {
            WaveLayoutData layout =
                ScriptableObject.CreateInstance<WaveLayoutData>();
            _createdObjects.Add(layout);
            return layout;
        }

        private SpawnableDefinition Spawnable(
            Vector2Int? footprint = null)
        {
            GameObject prefab = new GameObject("Prefab");
            _createdObjects.Add(prefab);

            SpawnableDefinition spawnable =
                ScriptableObject.CreateInstance<
                    SpawnableDefinition>();

            spawnable.Configure(
                "Test Enemy",
                prefab,
                SpawnableKind.Enemy,
                footprint ?? Vector2Int.one,
                Vector2Int.zero);

            _createdObjects.Add(spawnable);
            return spawnable;
        }

        private static WaveLayoutPlacement Placement(
            SpawnableDefinition spawnable,
            Vector2Int cell)
        {
            return new WaveLayoutPlacement
            {
                Spawnable = spawnable,
                Cell = cell
            };
        }
    }
}
