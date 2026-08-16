using UnityEditor;
using UnityEngine;

namespace MissNibiru.Information.Editor
{
    public sealed partial class InformationOrganizerWindow
    {
        private void DrawFaqPage()
        {
            using (EditorGUILayout.ScrollViewScope scroll =
                   new EditorGUILayout.ScrollViewScope(
                       faqScroll,
                       GUILayout.ExpandHeight(true)))
            {
                faqScroll = scroll.scrollPosition;

                using (new EditorGUILayout.VerticalScope(
                           EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(
                        "Information Organizer FAQ",
                        previewTitleStyle);

                    EditorGUILayout.LabelField(
                        "What each asset means and how the game uses it.",
                        wrapLabelStyle);
                }

                EditorGUILayout.HelpBox(
                    "Edit Fields is active when highlighted. Select an entry, " +
                    "then change the fields below it. Preview Entry is read-only.",
                    MessageType.Info);

                DrawFaqSection(
                    "Database",
                    "The master list of entries for one project or feature.",
                    "Assign it to InformationCollectionComponent. The game " +
                    "registers every entry inside it at startup.",
                    "Example: Main Game Information or Recipe Book.");

                DrawFaqSection(
                    "Entry",
                    "One thing the player can learn, collect or inspect.",
                    "Assign it to InformationSource on an interactable object. " +
                    "Collecting it reports this entry to the collection component.",
                    "Example: Healing Potion, letter, weapon or recipe.");

                DrawFaqSection(
                    "Type",
                    "A broad reusable kind of entry.",
                    "The game can retrieve collected entries by type. Use types " +
                    "for major rules or screens shared by many entries.",
                    "Example: Item, Document, Weapon or Recipe.");

                DrawFaqSection(
                    "Category",
                    "A narrower group inside or across types.",
                    "The game can retrieve collected entries by category. Use " +
                    "categories for filtering and smaller groupings.",
                    "Example: Potion, Poké Ball, Dessert or Evidence.");

                DrawFaqSection(
                    "Stable ID",
                    "The unique machine-readable name of an entry.",
                    "Save data uses this value to remember what was collected. " +
                    "Do not change it after shipping saved games.",
                    "Example: healing_potion.");

                DrawFaqSection(
                    "Information Pages",
                    "Optional extra screens of text and images.",
                    "Your UI can display these as document pages, recipe steps, " +
                    "lore sections or item details.",
                    "Example: ingredients, instructions and serving notes.");

                DrawFaqSection(
                    "Icon and Image",
                    "Visuals for lists and full details.",
                    "Icon is intended for compact UI. Image is intended for the " +
                    "main entry view. Your game UI decides how to show them.",
                    "Both are optional.");

                DrawFaqSection(
                    "Related Asset",
                    "An optional link to gameplay-specific data.",
                    "Use it to connect the information entry to another " +
                    "ScriptableObject, such as an item, weapon or recipe.",
                    "The organizer stores the link but does not run that asset.");

                DrawFaqSection(
                    "Word Warnings",
                    "Authoring limits for summaries and pages.",
                    "The tool warns when text is too long. It never cuts or " +
                    "changes the player's text.",
                    "Set a limit to zero to disable its warning.");

                EditorGUILayout.Space(6f);

                EditorGUILayout.HelpBox(
                    "The organizer creates and validates data. Your gameplay " +
                    "scripts and UI decide when entries are collected and how " +
                    "they appear to the player.",
                    MessageType.Info);
            }
        }

        private void DrawFaqSection(
            string heading,
            string meaning,
            string runtimeUse,
            string example)
        {
            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    heading,
                    EditorStyles.boldLabel);

                EditorGUILayout.LabelField(
                    meaning,
                    wrapLabelStyle);

                EditorGUILayout.Space(2f);
                EditorGUILayout.LabelField(
                    "In the game",
                    EditorStyles.miniBoldLabel);

                EditorGUILayout.LabelField(
                    runtimeUse,
                    wrapLabelStyle);

                EditorGUILayout.LabelField(
                    example,
                    EditorStyles.miniLabel);
            }
        }
    }
}
