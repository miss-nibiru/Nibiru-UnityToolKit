using System.Collections.Generic;
using MissNibiru.Information.Data;
using MissNibiru.Information.Editor;
using NUnit.Framework;
using UnityEngine;

namespace MissNibiru.Information.Tests
{
    public sealed class InformationOrganizerEditorTests
    {
        private readonly List<Object> createdObjects =
            new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object createdObject in createdObjects)
            {
                if (createdObject != null)
                    Object.DestroyImmediate(createdObject);
            }

            createdObjects.Clear();
        }

        [Test]
        public void CreateStableId_CleansNames()
        {
            Assert.AreEqual(
                "creme_brulee_recipe",
                InformationOrganizerUtility.CreateStableId(
                    "Crème Brûlée Recipe!"));

            Assert.AreEqual(
                "healing_potion",
                InformationOrganizerUtility.CreateStableId(
                    "HealingPotion"));
        }

        [Test]
        public void GenerateUniqueId_AddsNextSuffix()
        {
            InformationEntry first = Entry(
                "healing_potion",
                "Healing Potion");

            InformationEntry second = Entry(
                "healing_potion_2",
                "Healing Potion Copy");

            InformationDatabase database = Database(
                first,
                second);

            Assert.AreEqual(
                "healing_potion_3",
                InformationOrganizerUtility.GenerateUniqueId(
                    database,
                    "Healing Potion"));
        }

        [Test]
        public void CountWords_CountsReadableWords()
        {
            Assert.AreEqual(
                6,
                InformationOrganizerUtility.CountWords(
                    "Mix, bake, and don't burn it."));

            Assert.AreEqual(
                0,
                InformationOrganizerUtility.CountWords("  "));
        }

        [Test]
        public void MatchesSearch_UsesNameAndId()
        {
            InformationEntry entry = Entry(
                "recipe_tomato_soup",
                "Tomato Soup");

            Assert.IsTrue(
                InformationOrganizerUtility.MatchesSearch(
                    entry,
                    "tomato"));

            Assert.IsTrue(
                InformationOrganizerUtility.MatchesSearch(
                    entry,
                    "recipe_"));

            Assert.IsFalse(
                InformationOrganizerUtility.MatchesSearch(
                    entry,
                    "dessert"));
        }

        [Test]
        public void Validator_ReportsDuplicateIds()
        {
            InformationEntry first = Entry(
                "duplicate",
                "First");

            InformationEntry second = Entry(
                "duplicate",
                "Second");

            InformationDatabase database = Database(
                first,
                second);

            List<InformationOrganizerIssue> issues =
                InformationOrganizerValidator.Validate(
                    database,
                    false);

            Assert.IsTrue(
                issues.Exists(
                    issue =>
                        issue.Severity ==
                            InformationOrganizerSeverity.Error &&
                        issue.Message.Contains("Duplicate ID")));
        }

        [Test]
        public void Validator_ReportsMissingRequiredData()
        {
            InformationEntry entry = Entry(
                string.Empty,
                string.Empty);

            InformationDatabase database = Database(entry);

            List<InformationOrganizerIssue> issues =
                InformationOrganizerValidator.Validate(
                    database,
                    false);

            Assert.IsTrue(
                issues.Exists(
                    issue =>
                        issue.Severity ==
                            InformationOrganizerSeverity.Error &&
                        issue.Message.Contains("no ID")));

            Assert.IsTrue(
                issues.Exists(
                    issue =>
                        issue.Severity ==
                            InformationOrganizerSeverity.Warning &&
                        issue.Message.Contains("display name")));
        }

        private InformationEntry Entry(
            string id,
            string displayName)
        {
            InformationEntry entry =
                ScriptableObject.CreateInstance<
                    InformationEntry>();

            entry.Configure(id, displayName, string.Empty);
            createdObjects.Add(entry);
            return entry;
        }

        private InformationDatabase Database(
            params InformationEntry[] entries)
        {
            InformationDatabase database =
                ScriptableObject.CreateInstance<
                    InformationDatabase>();

            database.Configure(entries);
            createdObjects.Add(database);
            return database;
        }
    }
}
