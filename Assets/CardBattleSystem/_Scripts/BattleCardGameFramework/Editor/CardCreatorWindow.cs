using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudCode;

namespace csbcgf
{
    public class CardCreatorWindow : EditorWindow
    {
        private List<Type> m_AvailableCardTypes = new List<Type>();
        private List<Type> m_AvailableActionTypes = new List<Type>();
        private List<Type> m_AvailableReactionTypes = new List<Type>();

        // Virtual types for templates
        private const string CUSTOM_CODE_ACTION = "Custom C# Action";
        private const string BATTLECRY_REACTION = "Battlecry Template";
        private const string CUSTOM_CODE_REACTION = "Custom C# Reaction";

        [System.Serializable]
        private class ParameterConfig
        {
            public string name;
            public string typeName;
            public string value;
        }

        [System.Serializable]
        private class ActionConfig
        {
            public string typeFullName;
            public string customCode = "// Enter custom action code here";
            public List<ParameterConfig> parameters = new List<ParameterConfig>();

            public ActionConfig(string typeName)
            {
                typeFullName = typeName;
            }
        }

        [System.Serializable]
        private class ReactionConfig
        {
            public string typeFullName;
            public string triggerAction = "SummonMonsterAction";
            public string customCode = "// Enter custom reaction logic here";
            public List<ParameterConfig> parameters = new List<ParameterConfig>();

            public ReactionConfig(string typeName)
            {
                typeFullName = typeName;
            }
        }

        [System.Serializable]
        public class PlayerDeckData
        {
            public string Name = "New Deck";
            public List<int> CardIds = new List<int>();
        }

        private enum WindowView { Creator, CloudSave }
        private WindowView m_CurrentView = WindowView.Creator;

        // Creator Views
        private VisualElement m_CreatorView = null!;
        private VisualElement m_CloudSaveView = null!;

        // Cloud Save Data
        private List<ICard> m_Library = new List<ICard>();
        private List<PlayerDeckData> m_Decks = new List<PlayerDeckData>();
        private List<Type> m_AllICardTypes = new List<Type>();
        private string m_UploadStatus = "Idle";

        // Generic Info
private TextField m_ClassNameField = null!;
        private TextField m_NamespaceField = null!;
        private IntegerField m_ManaField = null!;
        private DropdownField m_CardTypeDropdown = null!;

        // Static Card Data Info
        private TextField m_StaticNameField = null!;
        private TextField m_StaticMechanicField = null!;
        private TextField m_StaticAbilityNameField = null!;
        private TextField m_StaticAbilityDescField = null!;
        private TextField m_StaticStoryField = null!;
        private TextField m_StaticClassField = null!;
        private TextField m_StaticRaceField = null!;
        private ObjectField m_ImageField = null!;

        // Monster Panel Elements
        private VisualElement m_MonsterPanel = null!;
        private IntegerField m_AttackField = null!;
        private IntegerField m_LifeField = null!;
        private ScrollView m_MonsterReactionsList = null!;
        private List<ReactionConfig> m_Reactions = new List<ReactionConfig>();

        // Spell Panel Elements
        private VisualElement m_SpellPanel = null!;
        private ScrollView m_SpellActionsList = null!;
        private List<ActionConfig> m_Actions = new List<ActionConfig>();

        // Output Directory Info
        private TextField m_DirectoryField = null!;
        private Label m_StatusLabel = null!;

        // Card Manager Elements
        private VisualElement m_ManagerPanel = null!;
        private ScrollView m_CardListScroll = null!;
        private List<CardRowData> m_CardRows = new List<CardRowData>();

        private class CardRowData
        {
            public string filePath;
            public string className;
            public string cardType;
            public int mana;
            public int attack;
            public int life;
            
            // Static data fields
            public string displayName;
            public string mechanic;
            public string abilityName;
            public string abilityDesc;
            public string story;
            public string className_static;
            public string race;
            public Sprite image;

            // UI References
            public TextField nameField;
            public IntegerField manaField;
            public IntegerField attackField;
            public IntegerField lifeField;
            public TextField displayNameField;
            public TextField mechanicField;
            public ObjectField imageField;

            public VisualElement root;
        }

        [MenuItem("Window/Battle Card Game Framework/Card Creator")]
        public static void ShowWindow()
        {
            CardCreatorWindow wnd = GetWindow<CardCreatorWindow>();
            wnd.titleContent = new GUIContent("Card Creator");
            wnd.minSize = new Vector2(450, 600);
        }

        private void DiscoverTypes()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            m_AvailableCardTypes = assemblies.SelectMany(a => a.GetTypes())
                .Where(t => typeof(Card).IsAssignableFrom(t) && !t.IsAbstract && t.Namespace != "csbcgf")
                .OrderBy(t => t.Name)
                .ToList();

            m_AllICardTypes = assemblies.SelectMany(a => a.GetTypes())
                .Where(t => typeof(ICard).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface && t.Namespace != "csbcgf")
                .OrderBy(t => t.Name)
                .ToList();

            m_AvailableActionTypes = assemblies.SelectMany(a => a.GetTypes())
.Where(t => (typeof(csbcgf.Action).IsAssignableFrom(t) || IsSubclassOfRawGeneric(typeof(csbcgf.Action<>), t)) 
                            && !t.IsAbstract && t.Namespace != "csbcgf")
                .OrderBy(t => t.Name)
                .ToList();

            m_AvailableReactionTypes = assemblies.SelectMany(a => a.GetTypes())
                .Where(t => !t.IsAbstract && IsSubclassOfRawGeneric(typeof(CardReaction<,,>), t))
                .OrderBy(t => t.Name)
                .ToList();
        }

        private static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur) return true;
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        private void SyncParameters(List<ParameterConfig> list, Type type)
        {
            list.Clear();
            if (type == null) return;

            // Pick the constructor with most parameters
            var ctor = type.GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (ctor != null)
            {
                foreach (var p in ctor.GetParameters())
                {
                    // Skip ICard/ParentCard parameter as it's usually handled by the base constructor
                    if (p.ParameterType == typeof(ICard) || p.Name.ToLower() == "card" || p.Name.ToLower() == "parentcard") 
                        continue;

                    string defaultValue = "";
                    if (p.ParameterType == typeof(int)) defaultValue = "1";
                    else if (p.ParameterType == typeof(string)) defaultValue = "\"\"";
                    else if (p.Name.ToLower().Contains("target") || p.Name.ToLower().Contains("living")) defaultValue = "target";
                    else if (p.Name.ToLower().Contains("player")) defaultValue = "game.State.ActivePlayer";

                    list.Add(new ParameterConfig
                    {
                        name = p.Name,
                        typeName = p.ParameterType.Name,
                        value = defaultValue
                    });
                }
            }
        }

        public void CreateGUI()
        {
            DiscoverTypes();
            LoadCloudSaveData();
            VisualElement root = rootVisualElement;

            // Load and apply stylesheet
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/_Scripts/BattleCardGameFramework/Editor/CardCreatorWindow.uss");
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }

            // Navigation Toolbar
            VisualElement toolbar = new VisualElement();
            toolbar.AddToClassList("toolbar");
            
            Button creatorBtn = new Button(() => SwitchView(WindowView.Creator)) { text = "Card Creator" };
            creatorBtn.AddToClassList("toolbar-button");
            toolbar.Add(creatorBtn);

            Button cloudSaveBtn = new Button(() => SwitchView(WindowView.CloudSave)) { text = "Cloud Save / Decks" };
            cloudSaveBtn.AddToClassList("toolbar-button");
            toolbar.Add(cloudSaveBtn);
            
            root.Add(toolbar);

            // Container for views
            m_CreatorView = new ScrollView();
            m_CreatorView.style.flexGrow = 1;
            BuildCreatorView(m_CreatorView);
            root.Add(m_CreatorView);

            m_CloudSaveView = new VisualElement();
            m_CloudSaveView.style.flexGrow = 1;
            m_CloudSaveView.AddToClassList("hidden");
            BuildCloudSaveView(m_CloudSaveView);
            root.Add(m_CloudSaveView);

            SwitchView(m_CurrentView);
        }

        private void SwitchView(WindowView view)
        {
            m_CurrentView = view;
            m_CreatorView.EnableInClassList("hidden", view != WindowView.Creator);
            m_CloudSaveView.EnableInClassList("hidden", view != WindowView.CloudSave);
            
            var buttons = rootVisualElement.Query<Button>(className: "toolbar-button").ToList();
            if (buttons.Count >= 2)
            {
                buttons[0].EnableInClassList("active", view == WindowView.Creator);
                buttons[1].EnableInClassList("active", view == WindowView.CloudSave);
            }
        }

        private void BuildCreatorView(VisualElement root)
        {
            // Title
            Label title = new Label("Card Creator");
            title.AddToClassList("title");
            root.Add(title);

            // 1. Generic Settings
            Box genericBox = CreateSectionBox("Generic Card Info");
            m_ClassNameField = new TextField("Class Name");
            m_ClassNameField.value = "MyCustomCard";
            m_ClassNameField.RegisterValueChangedCallback(evt => {
                string val = evt.newValue;
                string clean = Regex.Replace(val, @"[^a-zA-Z0-9_]", "");
                if (clean != val) m_ClassNameField.value = clean;
            });
            genericBox.Add(m_ClassNameField);

            m_NamespaceField = new TextField("Namespace");
            m_NamespaceField.value = "hearthstone";
            genericBox.Add(m_NamespaceField);

            m_ManaField = new IntegerField("Mana Cost");
            m_ManaField.value = 1;
            genericBox.Add(m_ManaField);

            m_CardTypeDropdown = new DropdownField("Card Type", 
                m_AvailableCardTypes.Select(t => t.Name).ToList(), 0);
            m_CardTypeDropdown.RegisterValueChangedCallback(evt => UpdateCardTypeView(m_AvailableCardTypes[m_CardTypeDropdown.index]));
            genericBox.Add(m_CardTypeDropdown);
            root.Add(genericBox);

            // 1.5 Static Card Data Configuration
            Box staticDataBox = CreateSectionBox("Card Static Data (ScriptableObject)");
            m_StaticNameField = new TextField("Card Name");
            m_StaticNameField.value = "My Custom Card";
            staticDataBox.Add(m_StaticNameField);

            m_StaticMechanicField = new TextField("Mechanic");
            staticDataBox.Add(m_StaticMechanicField);

            m_StaticAbilityNameField = new TextField("Ability Name");
            staticDataBox.Add(m_StaticAbilityNameField);

            m_StaticAbilityDescField = new TextField("Ability Description");
            staticDataBox.Add(m_StaticAbilityDescField);

            m_StaticStoryField = new TextField("Story");
            staticDataBox.Add(m_StaticStoryField);

            m_StaticClassField = new TextField("Class");
            staticDataBox.Add(m_StaticClassField);

            m_StaticRaceField = new TextField("Race");
            staticDataBox.Add(m_StaticRaceField);

            m_ImageField = new ObjectField("Card Image (Sprite)") { objectType = typeof(Sprite) };
            staticDataBox.Add(m_ImageField);

            root.Add(staticDataBox);

            // 2. Monster Properties Panel
            m_MonsterPanel = CreateSectionBox("Monster Configuration");
            m_AttackField = new IntegerField("Attack");
            m_AttackField.value = 1;
            m_MonsterPanel.Add(m_AttackField);

            m_LifeField = new IntegerField("Life");
            m_LifeField.value = 1;
            m_MonsterPanel.Add(m_LifeField);

            Label reactionTitle = new Label("Card Reactions / Events:");
            reactionTitle.AddToClassList("sub-header");
            reactionTitle.AddToClassList("reaction-header");
            m_MonsterPanel.Add(reactionTitle);

            m_MonsterReactionsList = new ScrollView();
            m_MonsterReactionsList.AddToClassList("list-scroll-view");
            m_MonsterReactionsList.AddToClassList("reaction-list");
            m_MonsterPanel.Add(m_MonsterReactionsList);

            VisualElement reactionButtons = new VisualElement();
            reactionButtons.AddToClassList("row");
            reactionButtons.AddToClassList("button-row");

            List<string> reactionOptions = new List<string> { BATTLECRY_REACTION, CUSTOM_CODE_REACTION };
            reactionOptions.AddRange(m_AvailableReactionTypes.Select(t => t.Name));
            
            PopupField<string> addReactionPopup = new PopupField<string>("Add Reaction", reactionOptions, 0);
            Button addReactionBtn = new Button(() => AddReaction(addReactionPopup.value)) { text = "+" };
            reactionButtons.Add(addReactionPopup);
            reactionButtons.Add(addReactionBtn);

            m_MonsterPanel.Add(reactionButtons);
            root.Add(m_MonsterPanel);

            // 3. Spell Properties Panel
            m_SpellPanel = CreateSectionBox("Spell Actions Configuration");
            Label actionsTitle = new Label("Cast Actions:");
            actionsTitle.AddToClassList("sub-header");
            m_SpellPanel.Add(actionsTitle);

            m_SpellActionsList = new ScrollView();
            m_SpellActionsList.AddToClassList("list-scroll-view");
            m_SpellActionsList.AddToClassList("action-list");
            m_SpellPanel.Add(m_SpellActionsList);

            VisualElement actionButtons = new VisualElement();
            actionButtons.AddToClassList("row");
            actionButtons.AddToClassList("button-row");

            List<string> actionOptions = new List<string> { CUSTOM_CODE_ACTION };
            actionOptions.AddRange(m_AvailableActionTypes.Select(t => t.Name));

            PopupField<string> addActionPopup = new PopupField<string>("Add Action", actionOptions, 0);
            Button addActionBtn = new Button(() => AddAction(addActionPopup.value)) { text = "+" };
            actionButtons.Add(addActionPopup);
            actionButtons.Add(addActionBtn);

            m_SpellPanel.Add(actionButtons);
            root.Add(m_SpellPanel);

            // 4. Output Folder Panel
            Box pathBox = CreateSectionBox("Output Path / Load");
            VisualElement pathRow = new VisualElement();
            pathRow.AddToClassList("row");
            pathRow.AddToClassList("path-row");

            m_DirectoryField = new TextField("Target Folder");
            m_DirectoryField.value = "Assets/_Card_Pool";
            pathRow.Add(m_DirectoryField);

            Button browseBtn = new Button(BrowseFolder) { text = "Browse" };
            pathRow.Add(browseBtn);
            pathBox.Add(pathRow);

            Button loadBtn = new Button(LoadExistingCard) { text = "Load Existing Card Script" };
            loadBtn.AddToClassList("load-button");
            pathBox.Add(loadBtn);

            root.Add(pathBox);

            // 5. Generate Button
            Button generateBtn = new Button(GenerateCardClass) { text = "Generate Card Class" };
            generateBtn.AddToClassList("generate-button");
            root.Add(generateBtn);

            // 6. Export CSV Button
            Button exportCsvBtn = new Button(ExportCardsToCsv) { text = "Export Cards to CSV" };
            exportCsvBtn.AddToClassList("export-button");
            root.Add(exportCsvBtn);

            // 7. Import CSV Button
            Button importCsvBtn = new Button(ImportCardsFromCsv) { text = "Import Cards from CSV" };
            importCsvBtn.AddToClassList("import-button");
            root.Add(importCsvBtn);

            // Status Label
            m_StatusLabel = new Label("Ready");
            m_StatusLabel.AddToClassList("status-label");
            root.Add(m_StatusLabel);

            // 8. Card Manager Section
            m_ManagerPanel = new VisualElement();
            m_ManagerPanel.AddToClassList("manager-section");
            
            Label managerTitle = new Label("Bulk Card Manager");
            managerTitle.AddToClassList("section-header");
            m_ManagerPanel.Add(managerTitle);

            VisualElement managerButtons = new VisualElement();
            managerButtons.AddToClassList("manager-buttons");
            
            Button refreshBtn = new Button(RefreshCardList) { text = "Refresh Card List" };
            Button saveAllBtn = new Button(SaveAllCards) { text = "Save All Changes" };
            managerButtons.Add(refreshBtn);
            managerButtons.Add(saveAllBtn);
            m_ManagerPanel.Add(managerButtons);

            m_CardListScroll = new ScrollView();
            m_CardListScroll.AddToClassList("card-list-scroll");
            m_ManagerPanel.Add(m_CardListScroll);

            root.Add(m_ManagerPanel);

            // Set up initial active type view
            if (m_AvailableCardTypes.Count > 0)
            {
                UpdateCardTypeView(m_AvailableCardTypes.FirstOrDefault(t => t.Name.Contains("Monster")) ?? m_AvailableCardTypes[0]);
            }
        }

        private Box CreateSectionBox(string headerText)
        {
            Box box = new Box();
            box.AddToClassList("section-box");

            Label label = new Label(headerText);
            label.AddToClassList("section-header");
            box.Add(label);

            return box;
        }

        private void UpdateCardTypeView(Type type)
        {
            if (type.Name.Contains("Monster"))
            {
                m_MonsterPanel.RemoveFromClassList("hidden");
                m_SpellPanel.AddToClassList("hidden");
            }
            else
            {
                m_MonsterPanel.AddToClassList("hidden");
                m_SpellPanel.RemoveFromClassList("hidden");
            }
        }

        private void BrowseFolder()
        {
            string selected = EditorUtility.OpenFolderPanel("Choose Output Directory", m_DirectoryField.value, "");
            if (!string.IsNullOrEmpty(selected))
            {
                // Convert absolute path to relative if possible
                if (selected.StartsWith(Application.dataPath))
                {
                    selected = "Assets" + selected.Substring(Application.dataPath.Length);
                }
                m_DirectoryField.value = selected;
            }
        }

        private void AddReaction(string typeName)
        {
            ReactionConfig config = new ReactionConfig(typeName);
            var type = m_AvailableReactionTypes.FirstOrDefault(t => t.Name == typeName);
            if (type != null)
            {
                SyncParameters(config.parameters, type);
            }
            m_Reactions.Add(config);
            RefreshReactionsListView();
        }

        private void RemoveReaction(ReactionConfig config)
        {
            m_Reactions.Remove(config);
            RefreshReactionsListView();
        }

        private void RefreshReactionsListView()
        {
            m_MonsterReactionsList.Clear();
            foreach (var reaction in m_Reactions)
            {
                VisualElement item = new VisualElement();
                item.AddToClassList("list-item-row-complex");

                VisualElement header = new VisualElement();
                header.AddToClassList("row");

                Label nameLabel = new Label(reaction.typeFullName);
                nameLabel.AddToClassList("reaction-item-name");
                header.Add(nameLabel);

                Button removeBtn = new Button(() => RemoveReaction(reaction)) { text = "X" };
                removeBtn.AddToClassList("remove-button");
                header.Add(removeBtn);
                item.Add(header);

                if (reaction.typeFullName == CUSTOM_CODE_REACTION)
                {
                    TextField triggerField = new TextField("Trigger Action");
                    triggerField.value = reaction.triggerAction;
                    triggerField.RegisterValueChangedCallback(evt => reaction.triggerAction = evt.newValue);
                    item.Add(triggerField);

                    TextField codeField = new TextField("Code");
                    codeField.value = reaction.customCode;
                    codeField.RegisterValueChangedCallback(evt => reaction.customCode = evt.newValue);
                    item.Add(codeField);
                }
                else if (reaction.typeFullName == BATTLECRY_REACTION)
                {
                    Label info = new Label("Standard Battlecry Template");
                    item.Add(info);
                }
                else
                {
                    foreach (var p in reaction.parameters)
                    {
                        TextField paramField = new TextField($"{p.name} ({p.typeName})");
                        paramField.value = p.value;
                        paramField.RegisterValueChangedCallback(evt => p.value = evt.newValue);
                        item.Add(paramField);
                    }
                }

                m_MonsterReactionsList.Add(item);
            }
        }

        private void AddAction(string typeName)
        {
            ActionConfig config = new ActionConfig(typeName);
            var type = m_AvailableActionTypes.FirstOrDefault(t => t.Name == typeName);
            if (type != null)
            {
                SyncParameters(config.parameters, type);
            }
            m_Actions.Add(config);
            RefreshActionsListView();
        }

        private void RemoveAction(ActionConfig config)
        {
            m_Actions.Remove(config);
            RefreshActionsListView();
        }

        private void RefreshActionsListView()
        {
            m_SpellActionsList.Clear();
            foreach (var action in m_Actions)
            {
                VisualElement item = new VisualElement();
                item.AddToClassList("list-item-row-complex");

                VisualElement header = new VisualElement();
                header.AddToClassList("row");

                Label nameLabel = new Label(action.typeFullName);
                nameLabel.AddToClassList("action-item-name");
                header.Add(nameLabel);

                Button removeBtn = new Button(() => RemoveAction(action)) { text = "X" };
                removeBtn.AddToClassList("remove-button");
                header.Add(removeBtn);
                item.Add(header);

                if (action.typeFullName == CUSTOM_CODE_ACTION)
                {
                    TextField codeField = new TextField("Code");
                    codeField.value = action.customCode;
                    codeField.RegisterValueChangedCallback(evt => action.customCode = evt.newValue);
                    item.Add(codeField);
                }
                else
                {
                    foreach (var p in action.parameters)
                    {
                        TextField paramField = new TextField($"{p.name} ({p.typeName})");
                        paramField.value = p.value;
                        paramField.RegisterValueChangedCallback(evt => p.value = evt.newValue);
                        item.Add(paramField);
                    }
                }

                m_SpellActionsList.Add(item);
            }
        }

        private void GenerateCardClass()
        {
            string className = m_ClassNameField.value.Trim();
            if (string.IsNullOrEmpty(className))
            {
                ShowError("Class Name cannot be empty.");
                return;
            }

            string targetDir = m_DirectoryField.value.Trim();
            if (!Directory.Exists(targetDir))
            {
                try
                {
                    Directory.CreateDirectory(targetDir);
                }
                catch (Exception e)
                {
                    ShowError($"Failed to create directory: {e.Message}");
                    return;
                }
            }

            string filePath = Path.Combine(targetDir, $"{className}.cs");
            string classContent = string.Empty;

            Type selectedType = m_AvailableCardTypes[m_CardTypeDropdown.index];
            if (selectedType.Name.Contains("Monster"))
            {
                classContent = GenerateMonsterCode(className, selectedType.Name);
            }
            else
            {
                bool isTargetful = selectedType.Name.Contains("Targetful");
                classContent = GenerateSpellCode(className, selectedType.Name, isTargetful);
            }

            try
            {
                File.WriteAllText(filePath, classContent);
                
                // Create and save CardStaticData ScriptableObject
                CardStaticData staticData = ScriptableObject.CreateInstance<CardStaticData>();
                staticData.Name = m_StaticNameField.value;
                staticData.Mechanic = m_StaticMechanicField.value;
                staticData.AbilityName = m_StaticAbilityNameField.value;
                staticData.AbilityDesc = m_StaticAbilityDescField.value;
                staticData.Story = m_StaticStoryField.value;
                staticData.Type = selectedType.Name.Contains("Monster") ? "UNIT" : "SPELL";
                staticData.Class = m_StaticClassField.value;
                staticData.Race = m_StaticRaceField.value;

                Sprite sprite = m_ImageField.value as Sprite;
                if (sprite != null)
                {
                    string spritePath = AssetDatabase.GetAssetPath(sprite);
                    string spriteGuid = AssetDatabase.AssetPathToGUID(spritePath);
                    
                    var addressableSettings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
                    if (addressableSettings != null)
                    {
                        var entry = addressableSettings.FindAssetEntry(spriteGuid);
                        if (entry == null)
                        {
                            entry = addressableSettings.CreateOrMoveEntry(spriteGuid, addressableSettings.DefaultGroup);
                            addressableSettings.SetDirty(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ModificationEvent.EntryAdded, entry, true);
                        }
                    }
                    
                    staticData.CardImage = new UnityEngine.AddressableAssets.AssetReferenceSprite(spriteGuid);
                }

                string staticDataPath = Path.Combine(targetDir, $"{className}StaticData.asset");
                AssetDatabase.CreateAsset(staticData, staticDataPath);
                AssetDatabase.SaveAssets();

                AssetDatabase.Refresh();
                ShowSuccess($"Created {className}.cs and {className}StaticData.asset at {targetDir}!");
            }
            catch (Exception e)
            {
                ShowError($"Failed to write file: {e.Message}");
            }
        }

        private string GenerateMonsterCode(string className, string baseClassName, int? customMana = null, int? customAttack = null, int? customLife = null, bool includeReactions = true)
        {
            string ns = m_NamespaceField.value.Trim();
            int mana = customMana ?? m_ManaField.value;
            int attack = customAttack ?? m_AttackField.value;
            int life = customLife ?? m_LifeField.value;

            string reactionLines = "";
            string nestedClasses = "";

            if (includeReactions)
            {
                foreach (var r in m_Reactions)
                {
                    if (r.typeFullName == BATTLECRY_REACTION)
                    {
                        reactionLines += $"            AddReaction(new {className}BattlecryReaction(this));\n";
                        nestedClasses += $@"
       public class {className}BattlecryReaction : CardReaction<HearthstoneGameState, HearthstoneGame, SummonMonsterAction>
        {{
            protected {className}BattlecryReaction() {{ }}

            public {className}BattlecryReaction(ICard card) : base(card) {{ }}

            public override void ReactAfter(HearthstoneGame game, SummonMonsterAction action)
            {{
                if (action.MonsterCard == ParentCard)
                {{
                    // Trigger battlecry logic here
                    game.Execute(new DrawCardAction(game.State.ActivePlayer));
                }}
            }}
        }}";
                    }
                    else if (r.typeFullName == CUSTOM_CODE_REACTION)
                    {
                        string trigger = r.triggerAction;
                        string rClassName = $"{className}_{trigger}Reaction";
                        reactionLines += $"            AddReaction(new {rClassName}(this));\n";
                        nestedClasses += $@"
       public class {rClassName} : CardReaction<HearthstoneGameState, HearthstoneGame, {trigger}>
        {{
            protected {rClassName}() {{ }}

            public {rClassName}(ICard card) : base(card) {{ }}

            public override void ReactAfter(HearthstoneGame game, {trigger} action)
            {{
                {r.customCode}
            }}
        }}";
                    }
                    else
                    {
                        string call = GetConstructorCall(r.typeFullName, r.parameters, true);
                        reactionLines += $"            AddReaction({call});\n";
                    }
                }
            }

            return $@"using csbcgf;
using System.Collections.Generic;

namespace {ns}
{{
    public class {className} : {baseClassName}
    {{
        protected {className}() {{ }}

        public {className}(bool _ = true) : base({mana}, {attack}, {life})
        {{
{reactionLines}        }}
{nestedClasses}
    }}
}}
";
        }

        private string GenerateSpellCode(string className, string baseClassName, bool isTargetful, int? customMana = null, bool includeActions = true)
        {
            string ns = m_NamespaceField.value.Trim();
            int mana = customMana ?? m_ManaField.value;

            string baseCompClass = isTargetful ? "HearthstoneTargetfulSpellCardComponent" : "HearthstoneTargetlessSpellCardComponent";
            string castMethodSig = isTargetful ? "public override void Cast(HearthstoneGame game, IStatContainer target)" : "public override void Cast(HearthstoneGame game)";

            string actionLines = "";
            if (includeActions)
            {
                foreach (var action in m_Actions)
                {
                    if (action.typeFullName == CUSTOM_CODE_ACTION)
                    {
                        actionLines += $"                {action.customCode}\n";
                    }
                    else
                    {
                        string call = GetConstructorCall(action.typeFullName, action.parameters, false);
                        actionLines += $"                game.Execute({call});\n";
                    }
                }
            }

            string targetMethod = "";
            if (isTargetful)
            {
                targetMethod = $@"
            public override ISet<IStatContainer> GetPotentialTargets(HearthstoneGameState gameState)
            {{
                HashSet<IStatContainer> targets = new HashSet<IStatContainer>();
                foreach (HearthstonePlayer player in gameState.Players)
                {{
                    targets.Add(player);
                    foreach (ICard card in player.GetCardCollection(CardCollectionKeys.Board).Cards)
                    {{
                        targets.Add((IStatContainer)card);
                    }}
                }}
                return targets;
            }}";
            }

            return $@"using csbcgf;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace {ns}
{{
    public class {className} : {baseClassName}
    {{
        protected {className}() {{ }}

        public {className}(bool _ = true) : base(_)
        {{
            AddComponent(new {className}Component());
        }}

        public class {className}Component : {baseCompClass}
        {{
            protected {className}Component() {{ }}

            public {className}Component(bool _ = true) : base({mana})
            {{
            }}

            {castMethodSig}
            {{
{actionLines}            }}
{targetMethod}
        }}
    }}
}}
";
        }

        private string GetConstructorCall(string typeName, List<ParameterConfig> parameters, bool isReaction)
        {
            var type = (isReaction ? m_AvailableReactionTypes : m_AvailableActionTypes)
                .FirstOrDefault(t => t.Name == typeName);

            string finalTypeName = typeName;
            if (type != null && type.IsGenericTypeDefinition)
            {
                finalTypeName += "<HearthstoneGameState>";
            }

            List<string> args = new List<string>();
            if (isReaction)
            {
                var ctor = type?.GetConstructors()
                    .OrderByDescending(c => c.GetParameters().Length)
                    .FirstOrDefault();
                if (ctor != null && ctor.GetParameters().Any(p => p.ParameterType == typeof(ICard) || p.Name.ToLower() == "card" || p.Name.ToLower() == "parentcard"))
                {
                    args.Add("this");
                }
            }

            args.AddRange(parameters.Select(p => p.value));
            return $"new {finalTypeName}({string.Join(", ", args)})";
        }

        private void BuildCloudSaveView(VisualElement root)
        {
// Toolbar for Cloud Save Status
            VisualElement statusRow = new VisualElement();
            statusRow.style.flexDirection = FlexDirection.Row;
            statusRow.style.paddingTop = 8;
            statusRow.style.paddingBottom = 8;
            
            Label statusLabel = new Label($"Status: {m_UploadStatus}");
            statusLabel.name = "upload-status-label";
            statusRow.Add(statusLabel);
            root.Add(statusRow);

            // Split View for Library and Decks
            VisualElement splitView = new VisualElement();
            splitView.AddToClassList("split-view");
            
            // 1. Library Panel
            VisualElement libraryPanel = new VisualElement();
            libraryPanel.AddToClassList("library-panel");
            Label libHeader = new Label("Card Library");
            libHeader.AddToClassList("section-header");
            libraryPanel.Add(libHeader);
            
            VisualElement addCardRow = new VisualElement();
            addCardRow.style.flexDirection = FlexDirection.Row;
            
            PopupField<string> typePopup = new PopupField<string>("Card Type", m_AllICardTypes.Select(t => t.Name).ToList(), 0);
            Button addBtn = new Button(() => AddToLibrary(m_AllICardTypes[typePopup.index])) { text = "Add" };
            addCardRow.Add(typePopup);
            addCardRow.Add(addBtn);
            libraryPanel.Add(addCardRow);

            ScrollView libraryList = new ScrollView();
            libraryList.name = "library-list";
            libraryList.style.flexGrow = 1;
            libraryPanel.Add(libraryList);
            
            splitView.Add(libraryPanel);

            // 2. Deck Panel
            VisualElement deckPanel = new VisualElement();
            deckPanel.AddToClassList("deck-panel");
            Label decksHeader = new Label("Player Decks");
            decksHeader.AddToClassList("section-header");
            deckPanel.Add(decksHeader);
            
            Button addDeckBtn = new Button(() => { m_Decks.Add(new PlayerDeckData()); RefreshDeckList(); }) { text = "Add New Deck" };
            deckPanel.Add(addDeckBtn);

            ScrollView deckList = new ScrollView();
            deckList.name = "deck-list";
            deckList.style.flexGrow = 1;
            deckPanel.Add(deckList);
            
            splitView.Add(deckPanel);
            root.Add(splitView);

            // Footer Buttons
            VisualElement footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.marginTop = 10;
            
            footer.Add(new Button(SaveCloudSaveData) { text = "Save Local" });
            footer.Add(new Button(LoadCloudSaveData) { text = "Load Local" });
            
            Button uploadBtn = new Button(() => _ = UploadDataAsync()) { text = "UPLOAD TO CLOUD SAVE" };
            uploadBtn.AddToClassList("upload-button");
            uploadBtn.style.flexGrow = 1;
            footer.Add(uploadBtn);
            
            root.Add(footer);

            RefreshLibraryList();
            RefreshDeckList();
        }

        private void SetUploadStatus(string status)
        {
            m_UploadStatus = status;
            if (m_CloudSaveView != null)
            {
                var label = m_CloudSaveView.Q<Label>("upload-status-label");
                if (label != null) label.text = $"Status: {status}";
            }
        }

        private void AddToLibrary(Type type)
        {
            ICard card = InstantiateCard(type);
            if (card != null)
            {
                if (card is Card concrete) concrete.Id = type.FullName.GetHashCode();
                m_Library.Add(card);
                RefreshLibraryList();
            }
        }

        private void RefreshLibraryList()
        {
            var list = m_CloudSaveView.Q<ScrollView>("library-list");
            if (list == null) return;
            list.Clear();

            foreach (var card in m_Library)
            {
                VisualElement row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 2;
                
                IntegerField idField = new IntegerField() { value = card.Id };
                idField.style.width = 40;
                idField.RegisterValueChangedCallback(evt => {
                    if (card is Card concrete) concrete.Id = evt.newValue;
                });
                row.Add(idField);
                
                row.Add(new Label(card.GetType().Name) { style = { marginLeft = 8, flexGrow = 1 } });
                
                Button removeBtn = new Button(() => { m_Library.Remove(card); RefreshLibraryList(); }) { text = "X" };
                removeBtn.AddToClassList("remove-button");
                row.Add(removeBtn);
                
                list.Add(row);
            }
        }

        private void RefreshDeckList()
        {
            var list = m_CloudSaveView.Q<ScrollView>("deck-list");
            if (list == null) return;
            list.Clear();

            foreach (var deck in m_Decks)
            {
                VisualElement deckBox = new VisualElement();
                deckBox.AddToClassList("deck-item");
                
                VisualElement header = new VisualElement();
                header.AddToClassList("deck-header");
                
                TextField nameField = new TextField() { value = deck.Name };
                nameField.AddToClassList("deck-name");
                nameField.RegisterValueChangedCallback(evt => deck.Name = evt.newValue);
                header.Add(nameField);
                
                Button removeBtn = new Button(() => { m_Decks.Remove(deck); RefreshDeckList(); }) { text = "X" };
                removeBtn.AddToClassList("remove-button");
                header.Add(removeBtn);
                deckBox.Add(header);
                
                TextField idsField = new TextField("Card IDs (CSV)") { value = string.Join(", ", deck.CardIds) };
                idsField.RegisterValueChangedCallback(evt => {
                    deck.CardIds = evt.newValue.Split(',')
                        .Select(s => int.TryParse(s.Trim(), out int id) ? id : -1)
                        .Where(id => id != -1).ToList();
                });
                deckBox.Add(idsField);
                
                var libraryOptions = m_Library.Select(c => $"ID {c.Id}: {c.GetType().Name}").ToList();
                if (libraryOptions.Count > 0)
                {
                    PopupField<string> addPopup = new PopupField<string>("Add from Library", libraryOptions, -1);
                    addPopup.RegisterValueChangedCallback(evt => {
                        if (addPopup.index >= 0) {
                            deck.CardIds.Add(m_Library[addPopup.index].Id);
                            RefreshDeckList();
                        }
                    });
                    deckBox.Add(addPopup);
                }
                
                list.Add(deckBox);
            }
        }

        private void SaveCloudSaveData()
        {
            string libraryJson = JsonSerializer.ToJson(m_Library);
            string decksJson = Newtonsoft.Json.JsonConvert.SerializeObject(m_Decks);
            EditorPrefs.SetString("CardCreator_Library", libraryJson);
            EditorPrefs.SetString("CardCreator_Decks", decksJson);
            m_UploadStatus = "Local data saved.";
        }

        private void LoadCloudSaveData()
        {
            string libraryJson = EditorPrefs.GetString("CardCreator_Library", "");
            string decksJson = EditorPrefs.GetString("CardCreator_Decks", "");
            
            if (!string.IsNullOrEmpty(libraryJson))
                m_Library = JsonSerializer.FromJson<List<ICard>>(libraryJson) ?? new List<ICard>();
            
            if (!string.IsNullOrEmpty(decksJson))
                m_Decks = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlayerDeckData>>(decksJson) ?? new List<PlayerDeckData>();
            
            m_UploadStatus = "Local data loaded.";
            if (m_CloudSaveView != null) {
                RefreshLibraryList();
                RefreshDeckList();
            }
        }

        private async Task UploadDataAsync()
        {
            SetUploadStatus("Initializing Services...");
            try
            {
                if (Unity.Services.Core.UnityServices.State == Unity.Services.Core.ServicesInitializationState.Uninitialized)
                    await Unity.Services.Core.UnityServices.InitializeAsync();

                if (!Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn)
                {
                    SetUploadStatus("Signing in...");
                    await Unity.Services.Authentication.AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                SetUploadStatus("Uploading Global Library...");
                
                Dictionary<int, ICard> libraryDict = m_Library.ToDictionary(c => c.Id, c => c);
                string libraryJson = JsonSerializer.ToJson(libraryDict);

                // Call Cloud Code to save global library to Game Data (Custom)
                await Unity.Services.CloudCode.CloudCodeService.Instance.CallModuleEndpointAsync(
                    "CloudCodeCardGame", 
                    "SaveGameData", 
                    new Dictionary<string, object> { { "key", "CardLibrary" }, { "value", libraryJson } }
                );

                SetUploadStatus("Uploading Player Decks...");
                
                Dictionary<string, List<int>> decksDict = m_Decks.ToDictionary(d => d.Name, d => d.CardIds);
                string decksJson = JsonSerializer.ToJson(decksDict);

                var data = new Dictionary<string, object> { { "PlayerDecks", decksJson } };
                await Unity.Services.CloudSave.CloudSaveService.Instance.Data.Player.SaveAsync(data);

                SetUploadStatus("Upload Successful!");
}
            catch (Exception e)
            {
                SetUploadStatus($"Upload Failed: {e.Message}");
                Debug.LogError(e);
            }
        }

        private void LoadExistingCard()
        {
            string path = EditorUtility.OpenFilePanel("Select Card Script", m_DirectoryField.value, "cs");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                string content = File.ReadAllText(path);
                string className = Path.GetFileNameWithoutExtension(path);
                string directory = Path.GetDirectoryName(path);
                
                if (directory.StartsWith(Application.dataPath))
                {
                    directory = "Assets" + directory.Substring(Application.dataPath.Length);
                }

                // Parse Namespace
                var nsMatch = Regex.Match(content, @"namespace\s+([a-zA-Z0-9_\.]+)");
                if (nsMatch.Success) m_NamespaceField.value = nsMatch.Groups[1].Value;

                // Parse Base Class and Type
                var classMatch = Regex.Match(content, @"public\s+class\s+" + className + @"\s+:\s+([a-zA-Z0-9_]+)");
                if (classMatch.Success)
                {
                    string baseClass = classMatch.Groups[1].Value;
                    int typeIndex = m_AvailableCardTypes.FindIndex(t => t.Name == baseClass);
                    if (typeIndex != -1)
                    {
                        m_CardTypeDropdown.index = typeIndex;
                        UpdateCardTypeView(m_AvailableCardTypes[typeIndex]);
                    }

                    // Parse Stats
                    if (baseClass.Contains("Monster"))
                    {
                        var statsMatch = Regex.Match(content, @"base\(([0-9]+),\s*([0-9]+),\s*([0-9]+)\)");
                        if (statsMatch.Success)
                        {
                            m_ManaField.value = int.Parse(statsMatch.Groups[1].Value);
                            m_AttackField.value = int.Parse(statsMatch.Groups[2].Value);
                            m_LifeField.value = int.Parse(statsMatch.Groups[3].Value);
                        }
                    }
                    else
                    {
                        var statsMatch = Regex.Match(content, @"base\(([0-9]+)\)");
                        if (statsMatch.Success)
                        {
                            m_ManaField.value = int.Parse(statsMatch.Groups[1].Value);
                        }
                    }
                }

                m_ClassNameField.value = className;
                m_DirectoryField.value = directory;

                // Load Static Data if exists
                string staticDataPath = Path.Combine(directory, $"{className}StaticData.asset");
                CardStaticData staticData = AssetDatabase.LoadAssetAtPath<CardStaticData>(staticDataPath);
                if (staticData != null)
                {
                    m_StaticNameField.value = staticData.Name;
                    m_StaticMechanicField.value = staticData.Mechanic;
                    m_StaticAbilityNameField.value = staticData.AbilityName;
                    m_StaticAbilityDescField.value = staticData.AbilityDesc;
                    m_StaticStoryField.value = staticData.Story;
                    m_StaticClassField.value = staticData.Class;
                    m_StaticRaceField.value = staticData.Race;
                    
                    if (staticData.CardImage != null && staticData.CardImage.RuntimeKeyIsValid())
                    {
                        string spritePath = AssetDatabase.GUIDToAssetPath(staticData.CardImage.AssetGUID);
                        m_ImageField.value = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    }
                }

                ShowSuccess($"Loaded card {className}!");
            }
            catch (Exception e)
            {
                ShowError($"Failed to load card: {e.Message}");
            }
        }

        private string ExtractValue(string content, string pattern)
        {
            var match = Regex.Match(content, pattern);
            return match.Success ? match.Groups[1].Value : "";
        }

        private void RefreshCardList()
        {
            string targetDir = m_DirectoryField.value.Trim();
            if (!Directory.Exists(targetDir))
            {
                ShowError("Target directory does not exist.");
                return;
            }

            m_CardListScroll.Clear();
            m_CardRows.Clear();

            // Add Header Row
            CreateCardHeaderUI();

            string[] files = Directory.GetFiles(targetDir, "*.cs");
            foreach (string file in files)
            {
                try
                {
                    string content = File.ReadAllText(file);
                    string className = Path.GetFileNameWithoutExtension(file);
                    
                    var classMatch = Regex.Match(content, @"public\s+class\s+" + className + @"\s+:\s+([a-zA-Z0-9_]+)");
                    if (!classMatch.Success) continue;

                    string baseClass = classMatch.Groups[1].Value;
                    CardRowData data = new CardRowData { filePath = file, className = className, cardType = baseClass };

                    // Parse Stats
                    if (baseClass.Contains("Monster"))
                    {
                        var statsMatch = Regex.Match(content, @"base\(([0-9]+),\s*([0-9]+),\s*([0-9]+)\)");
                        if (statsMatch.Success)
                        {
                            data.mana = int.Parse(statsMatch.Groups[1].Value);
                            data.attack = int.Parse(statsMatch.Groups[2].Value);
                            data.life = int.Parse(statsMatch.Groups[3].Value);
                        }
                    }
                    else
                    {
                        var statsMatch = Regex.Match(content, @"base\(([0-9]+)\)");
                        if (statsMatch.Success)
                        {
                            data.mana = int.Parse(statsMatch.Groups[1].Value);
                        }
                    }

                    // Load Static Data
                    string staticDataPath = Path.Combine(targetDir, $"{className}StaticData.asset");
                    CardStaticData staticData = AssetDatabase.LoadAssetAtPath<CardStaticData>(staticDataPath);
                    if (staticData != null)
                    {
                        data.displayName = staticData.Name;
                        data.mechanic = staticData.Mechanic;
                        data.abilityName = staticData.AbilityName;
                        data.abilityDesc = staticData.AbilityDesc;
                        data.story = staticData.Story;
                        data.className_static = staticData.Class;
                        data.race = staticData.Race;
                        if (staticData.CardImage != null && staticData.CardImage.RuntimeKeyIsValid())
                        {
                            string spritePath = AssetDatabase.GUIDToAssetPath(staticData.CardImage.AssetGUID);
                            data.image = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                        }
                    }

                    CreateCardRowUI(data);
                    m_CardRows.Add(data);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to parse card file {file}: {e.Message}");
                }
            }
            
            ShowSuccess($"Loaded {m_CardRows.Count} cards into manager.");
        }

        private void CreateCardHeaderUI()
        {
            VisualElement header = new VisualElement();
            header.AddToClassList("card-row");
            header.AddToClassList("card-header-row");

            var labels = new string[] { "Class Name", "Mana", "Atk", "Life", "Display Name", "Mechanic", "Image" };
            var classes = new string[] { "card-row-name", "card-row-stat", "card-row-stat", "card-row-stat", "card-row-text", "card-row-text", "card-row-image-header" };

            for (int i = 0; i < labels.Length; i++)
            {
                Label label = new Label(labels[i]);
                label.AddToClassList("card-header-label");
                label.AddToClassList(classes[i]);
                header.Add(label);
            }

            m_CardListScroll.Add(header);
        }

        private void CreateCardRowUI(CardRowData data)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("card-row");

            data.nameField = new TextField();
            data.nameField.value = data.className;
            data.nameField.isReadOnly = true;
            data.nameField.AddToClassList("card-row-name");
            row.Add(data.nameField);

            data.manaField = new IntegerField();
            data.manaField.value = data.mana;
            data.manaField.AddToClassList("card-row-stat");
            row.Add(data.manaField);

            if (data.cardType.Contains("Monster"))
            {
                data.attackField = new IntegerField();
                data.attackField.value = data.attack;
                data.attackField.AddToClassList("card-row-stat");
                row.Add(data.attackField);

                data.lifeField = new IntegerField();
                data.lifeField.value = data.life;
                data.lifeField.AddToClassList("card-row-stat");
                row.Add(data.lifeField);
            }
            else
            {
                // Placeholder for alignment
                var p1 = new VisualElement(); p1.style.width = 54; row.Add(p1);
                var p2 = new VisualElement(); p2.style.width = 54; row.Add(p2);
            }

            data.displayNameField = new TextField();
            data.displayNameField.value = data.displayName;
            data.displayNameField.AddToClassList("card-row-text");
            row.Add(data.displayNameField);

            data.mechanicField = new TextField();
            data.mechanicField.value = data.mechanic;
            data.mechanicField.AddToClassList("card-row-text");
            row.Add(data.mechanicField);

            data.imageField = new ObjectField() { objectType = typeof(Sprite) };
            data.imageField.value = data.image;
            data.imageField.style.width = 100;
            row.Add(data.imageField);

            m_CardListScroll.Add(row);
        }

        private void SaveAllCards()
        {
            int savedCount = 0;
            foreach (var data in m_CardRows)
            {
                try
                {
                    string className = data.className;
                    string targetDir = m_DirectoryField.value.Trim();
                    
                    int mana = data.manaField.value;
                    int attack = data.attackField != null ? data.attackField.value : 0;
                    int life = data.lifeField != null ? data.lifeField.value : 0;

                    string classContent;
                    if (data.cardType.Contains("Monster"))
                    {
                        classContent = GenerateMonsterCode(className, data.cardType, mana, attack, life, false);
                    }
                    else
                    {
                        bool isTargetful = data.cardType.Contains("Targetful");
                        classContent = GenerateSpellCode(className, data.cardType, isTargetful, mana, false);
                    }

                    File.WriteAllText(data.filePath, classContent);

                    string staticDataPath = Path.Combine(targetDir, $"{className}StaticData.asset");
                    CardStaticData staticData = AssetDatabase.LoadAssetAtPath<CardStaticData>(staticDataPath);
                    if (staticData != null)
                    {
                        staticData.Name = data.displayNameField.value;
                        staticData.Mechanic = data.mechanicField.value;
                        
                        Sprite sprite = data.imageField.value as Sprite;
                        if (sprite != null)
                        {
                            string spriteGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sprite));
                            staticData.CardImage = new UnityEngine.AddressableAssets.AssetReferenceSprite(spriteGuid);
                        }
                        
                        EditorUtility.SetDirty(staticData);
                    }
                    savedCount++;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to save card {data.className}: {e.Message}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ShowSuccess($"Saved {savedCount} cards.");
        }

        private void ShowError(string msg)
        {
            m_StatusLabel.text = $"Error: {msg}";
            m_StatusLabel.RemoveFromClassList("success");
            m_StatusLabel.AddToClassList("error");
        }

        private void ShowSuccess(string msg)
        {
            m_StatusLabel.text = msg;
            m_StatusLabel.RemoveFromClassList("error");
            m_StatusLabel.AddToClassList("success");
        }

        private void ExportCardsToCsv()
        {
            string path = EditorUtility.SaveFilePanel("Export Cards to CSV", "", "cards.csv", "csv");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var cardTypes = assemblies.SelectMany(a => a.GetTypes())
                    .Where(t => typeof(Card).IsAssignableFrom(t) && !t.IsAbstract && t.Namespace != "csbcgf")
                    .OrderBy(t => t.Name)
                    .ToList();

                List<string> csvLines = new List<string>
                {
                    "Class Name,Card Type,Mana,Attack,Life,Display Name,Mechanic,Ability Name,Ability Description,Story,Class,Race,Image Path"
                };

                int exportedCount = 0;
                foreach (var type in cardTypes)
                {
                    Card card = InstantiateCard(type);
                    string className = type.Name;
                    string cardType = "Card";
                    string manaStr = "";
                    string attackStr = "";
                    string lifeStr = "";

                    if (card != null)
                    {
                        // Determine card type
                        if (card.GetType().Name.Contains("Monster") || card.GetType().BaseType?.Name.Contains("Monster") == true)
                        {
                            cardType = "Monster";
                        }
                        else if (card.GetType().Name.Contains("Spell") || card.GetType().BaseType?.Name.Contains("Spell") == true)
                        {
                            cardType = "Spell";
                        }

                        // Retrieve stats
                        manaStr = card.GetValue("Mana").ToString();

                        if (cardType == "Monster")
                        {
                            attackStr = card.GetValue("Attack").ToString();
                            lifeStr = card.GetValue("Life").ToString();
                        }
                    }
                    else
                    {
                        // Fallback using reflection hierarchy if instantiation fails
                        Type t = type;
                        while (t != null && t != typeof(object))
                        {
                            if (t.Name.Contains("Monster"))
                            {
                                cardType = "Monster";
                                break;
                            }
                            if (t.Name.Contains("Spell"))
                            {
                                cardType = "Spell";
                                break;
                            }
                            t = t.BaseType;
                        }
                    }

                    // Static Data fields
                    string displayName = "";
                    string mechanic = "";
                    string abilityName = "";
                    string abilityDesc = "";
                    string story = "";
                    string @class = "";
                    string race = "";
                    string imagePath = "";

                    string[] staticDataGuids = AssetDatabase.FindAssets($"t:CardStaticData {className}StaticData");
                    if (staticDataGuids.Length > 0)
                    {
                        string staticDataPath = AssetDatabase.GUIDToAssetPath(staticDataGuids[0]);
                        CardStaticData staticData = AssetDatabase.LoadAssetAtPath<CardStaticData>(staticDataPath);
                        if (staticData != null)
                        {
                            displayName = staticData.Name;
                            mechanic = staticData.Mechanic;
                            abilityName = staticData.AbilityName;
                            abilityDesc = staticData.AbilityDesc;
                            story = staticData.Story;
                            @class = staticData.Class;
                            race = staticData.Race;
                            if (staticData.CardImage != null && staticData.CardImage.RuntimeKeyIsValid())
                            {
                                // Try to get path from GUID in AssetReference
                                string guid = staticData.CardImage.AssetGUID;
                                imagePath = AssetDatabase.GUIDToAssetPath(guid);
                            }
                        }
                    }

                    csvLines.Add($"{EscapeCSV(className)},{EscapeCSV(cardType)},{EscapeCSV(manaStr)},{EscapeCSV(attackStr)},{EscapeCSV(lifeStr)}," +
                                 $"{EscapeCSV(displayName)},{EscapeCSV(mechanic)},{EscapeCSV(abilityName)},{EscapeCSV(abilityDesc)},{EscapeCSV(story)}," +
                                 $"{EscapeCSV(@class)},{EscapeCSV(race)},{EscapeCSV(imagePath)}");
                    exportedCount++;
                }

                File.WriteAllLines(path, csvLines);
                ShowSuccess($"Successfully exported {exportedCount} cards to CSV at: {path}");
            }
            catch (Exception e)
            {
                ShowError($"Failed to export CSV: {e.Message}");
            }
        }

        private static Card InstantiateCard(Type type)
        {
            try
            {
                var ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (ctor != null)
                {
                    return (Card)ctor.Invoke(null);
                }
            }
            catch { }

            var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .OrderBy(c => c.GetParameters().Length)
                .ToList();

            foreach (var ctor in constructors)
            {
                try
                {
                    var parameters = ctor.GetParameters();
                    object[] args = new object[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var pType = parameters[i].ParameterType;
                        if (pType == typeof(bool)) args[i] = true;
                        else if (pType == typeof(int)) args[i] = 1;
                        else if (pType == typeof(uint)) args[i] = 1u;
                        else if (pType == typeof(string)) args[i] = "";
                        else args[i] = pType.IsValueType ? Activator.CreateInstance(pType) : null;
                    }
                    return (Card)ctor.Invoke(args);
                }
                catch { }
            }

            return null;
        }

        private string EscapeCSV(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            if (str.Contains(",") || str.Contains("\"") || str.Contains("\n") || str.Contains("\r"))
            {
                return "\"" + str.Replace("\"", "\"\"") + "\"";
            }
            return str;
        }

        private void ImportCardsFromCsv()
        {
            string path = EditorUtility.OpenFilePanel("Import Cards from CSV", "", "csv");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(path);
                if (lines.Length == 0)
                {
                    ShowError("Selected CSV file is empty.");
                    return;
                }

                // Parse header to find indices
                List<string> headers = ParseCsvLine(lines[0]);
                int classNameIndex = -1;
                int typeIndex = -1;
                int manaIndex = -1;
                int attackIndex = -1;
                int lifeIndex = -1;
                int displayNameIndex = -1;
                int mechanicIndex = -1;
                int abilityNameIndex = -1;
                int abilityDescIndex = -1;
                int storyIndex = -1;
                int classIndex = -1;
                int raceIndex = -1;
                int imagePathIndex = -1;

                for (int i = 0; i < headers.Count; i++)
                {
                    string h = headers[i].ToLower().Replace(" ", "").Replace("_", "");
                    if (h == "classname" || h == "cardname" || h == "name") classNameIndex = i;
                    else if (h == "cardtype" || h == "type") typeIndex = i;
                    else if (h == "mana" || h == "manacost") manaIndex = i;
                    else if (h == "attack") attackIndex = i;
                    else if (h == "life" || h == "health") lifeIndex = i;
                    else if (h == "displayname") displayNameIndex = i;
                    else if (h == "mechanic") mechanicIndex = i;
                    else if (h == "abilityname") abilityNameIndex = i;
                    else if (h == "abilitydescription" || h == "abilitydesc") abilityDescIndex = i;
                    else if (h == "story" || h == "flavor" || h == "flavortext") storyIndex = i;
                    else if (h == "class") classIndex = i;
                    else if (h == "race") raceIndex = i;
                    else if (h == "imagepath" || h == "image") imagePathIndex = i;
                }

                // Fallback to defaults if headers are not matched
                if (classNameIndex == -1) classNameIndex = 0;
                if (typeIndex == -1) typeIndex = 1;
                if (manaIndex == -1) manaIndex = 2;
                if (attackIndex == -1) attackIndex = 3;
                if (lifeIndex == -1) lifeIndex = 4;

                string targetDir = m_DirectoryField.value.Trim();
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                int importedCount = 0;
                for (int rowIndex = 1; rowIndex < lines.Length; rowIndex++)
                {
                    string line = lines[rowIndex];
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    List<string> row = ParseCsvLine(line);
                    if (row.Count <= Math.Max(classNameIndex, typeIndex)) continue;

                    string cardName = row[classNameIndex].Trim();
                    if (string.IsNullOrEmpty(cardName)) continue;

                    // Clean the class name (must be a valid C# identifier)
                    string className = Regex.Replace(cardName, @"[^a-zA-Z0-9_]", "");
                    if (string.IsNullOrEmpty(className)) continue;

                    string cardType = row.Count > typeIndex ? row[typeIndex].Trim() : "Monster";

                    int mana = 1;
                    if (row.Count > manaIndex && int.TryParse(row[manaIndex], out int parsedMana))
                    {
                        mana = parsedMana;
                    }

                    int attack = 1;
                    if (row.Count > attackIndex && int.TryParse(row[attackIndex], out int parsedAttack))
                    {
                        attack = parsedAttack;
                    }

                    int life = 1;
                    if (row.Count > lifeIndex && int.TryParse(row[lifeIndex], out int parsedLife))
                    {
                        life = parsedLife;
                    }

                    string baseClassName = "HearthstoneMonsterCard";
                    bool isMonster = true;
                    bool isTargetful = false;

                    string typeLower = cardType.ToLower();
                    if (typeLower.Contains("spell"))
                    {
                        isMonster = false;
                        if (typeLower.Contains("targetful"))
                        {
                            isTargetful = true;
                            var matchedType = m_AvailableCardTypes.FirstOrDefault(t => t.Name.Contains("Targetful"));
                            if (matchedType != null) baseClassName = matchedType.Name;
                            else baseClassName = "HearthstoneTargetfulSpellCard";
                        }
                        else
                        {
                            var matchedType = m_AvailableCardTypes.FirstOrDefault(t => t.Name.Contains("Targetless") || t.Name.Contains("Spell"));
                            if (matchedType != null) baseClassName = matchedType.Name;
                            else baseClassName = "HearthstoneTargetlessSpellCard";
                        }
                    }
                    else
                    {
                        var matchedType = m_AvailableCardTypes.FirstOrDefault(t => t.Name.Contains("Monster"));
                        if (matchedType != null) baseClassName = matchedType.Name;
                    }

                    string filePath = Path.Combine(targetDir, $"{className}.cs");
                    string classContent;

                    if (isMonster)
                    {
                        classContent = GenerateMonsterCode(className, baseClassName, mana, attack, life, false);
                    }
                    else
                    {
                        classContent = GenerateSpellCode(className, baseClassName, isTargetful, mana, false);
                    }

                    File.WriteAllText(filePath, classContent);

                    // Handle CardStaticData
                    CardStaticData staticData = ScriptableObject.CreateInstance<CardStaticData>();
                    staticData.Name = displayNameIndex != -1 && row.Count > displayNameIndex ? row[displayNameIndex] : className;
                    staticData.Mechanic = mechanicIndex != -1 && row.Count > mechanicIndex ? row[mechanicIndex] : "";
                    staticData.AbilityName = abilityNameIndex != -1 && row.Count > abilityNameIndex ? row[abilityNameIndex] : "";
                    staticData.AbilityDesc = abilityDescIndex != -1 && row.Count > abilityDescIndex ? row[abilityDescIndex] : "";
                    staticData.Story = storyIndex != -1 && row.Count > storyIndex ? row[storyIndex] : "";
                    staticData.Type = isMonster ? "UNIT" : "SPELL";
                    staticData.Class = classIndex != -1 && row.Count > classIndex ? row[classIndex] : "";
                    staticData.Race = raceIndex != -1 && row.Count > raceIndex ? row[raceIndex] : "";

                    if (imagePathIndex != -1 && row.Count > imagePathIndex)
                    {
                        string imagePath = row[imagePathIndex];
                        if (!string.IsNullOrEmpty(imagePath))
                        {
                            string guid = AssetDatabase.AssetPathToGUID(imagePath);
                            if (!string.IsNullOrEmpty(guid))
                            {
                                var addressableSettings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
                                if (addressableSettings != null)
                                {
                                    var entry = addressableSettings.FindAssetEntry(guid);
                                    if (entry == null)
                                    {
                                        entry = addressableSettings.CreateOrMoveEntry(guid, addressableSettings.DefaultGroup);
                                        addressableSettings.SetDirty(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ModificationEvent.EntryAdded, entry, true);
                                    }
                                }
                                staticData.CardImage = new UnityEngine.AddressableAssets.AssetReferenceSprite(guid);
                            }
                        }
                    }

                    string staticDataPath = Path.Combine(targetDir, $"{className}StaticData.asset");
                    AssetDatabase.CreateAsset(staticData, staticDataPath);

                    importedCount++;
                }

                if (importedCount > 0)
                {
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    ShowSuccess($"Successfully imported and created {importedCount} card classes and static data at: {targetDir}");
                }
                else
                {
                    ShowError("No valid card rows were found to import.");
                }
            }
            catch (Exception e)
            {
                ShowError($"Failed to import CSV: {e.Message}");
            }
        }

        private List<string> ParseCsvLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            string current = "";
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                    {
                        current += '\"';
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.Trim());
                    current = "";
                }
                else
                {
                    current += c;
                }
            }
            result.Add(current.Trim());
            return result;
        }
    }
}
