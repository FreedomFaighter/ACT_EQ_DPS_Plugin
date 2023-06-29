using Advanced_Combat_Tracker;
using EverQuestDPSPlugin.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
/*
* Project: EverQuest DPS Plugin
* Original: EverQuest 2 English DPS Localization plugin developed by EQAditu
* Description: Missing from the arsenal of the plugin based Advanced Combat Tracker to track EverQuest's current combat messages.  Ignores chat as that is displayed in game.
*/


#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

namespace EverQuestDPSPlugin
{
    public class EverQuestDPSPlugin : UserControl, IActPluginV1, IEverQuestDPSPlugin
    {
        #region Designer generated code (Avoid editing)
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.varianceChkBx = new System.Windows.Forms.CheckBox();
            this.nonMatchVisibleChkbx = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // varianceChkBx
            // 
            this.varianceChkBx.AutoSize = true;
            this.varianceChkBx.Location = new System.Drawing.Point(19, 26);
            this.varianceChkBx.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.varianceChkBx.Name = "varianceChkBx";
            this.varianceChkBx.Size = new System.Drawing.Size(398, 20);
            this.varianceChkBx.TabIndex = 19;
            this.varianceChkBx.Text = "Population Variance (checked)/Sample Variance (unchecked)";
            this.varianceChkBx.UseVisualStyleBackColor = true;
            this.varianceChkBx.CheckedChanged += new System.EventHandler(this.VarianceChkBx_CheckedChanged);
            // 
            // nonMatchVisibleChkbx
            // 
            this.nonMatchVisibleChkbx.AutoSize = true;
            this.nonMatchVisibleChkbx.Location = new System.Drawing.Point(19, 71);
            this.nonMatchVisibleChkbx.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.nonMatchVisibleChkbx.Name = "nonMatchVisibleChkbx";
            this.nonMatchVisibleChkbx.Size = new System.Drawing.Size(132, 20);
            this.nonMatchVisibleChkbx.TabIndex = 20;
            this.nonMatchVisibleChkbx.Text = "NonMatch visible";
            this.nonMatchVisibleChkbx.UseVisualStyleBackColor = true;
            this.nonMatchVisibleChkbx.CheckedChanged += new System.EventHandler(this.nonMatchVisible_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(19, 134);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(54, 16);
            this.label1.TabIndex = 21;
            this.label1.Text = "License";
            // 
            // richTextBox1
            // 
            this.richTextBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.richTextBox1.Location = new System.Drawing.Point(22, 171);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(375, 305);
            this.richTextBox1.TabIndex = 22;
            this.richTextBox1.Text = "";
            // 
            // EverQuestDPSPlugin
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.nonMatchVisibleChkbx);
            this.Controls.Add(this.varianceChkBx);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MinimumSize = new System.Drawing.Size(267, 492);
            this.Name = "EverQuestDPSPlugin";
            this.Size = new System.Drawing.Size(421, 492);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        #endregion

        #region Class Members
        delegate void matchParse(Match regexMatch);
        List<Tuple<Color, Regex>> regexTupleList;
        List<RegexAndParseMethod> regexAndParseMethodList;
        Regex selfCheck;
        Regex possesive;
        Regex tellsregex;
        bool nonMatchVisible = false; //for keep track of whether or not the non matching form is displayed
        bool populationVariance; //for keeping track of whether population variance or sample variance is displayed
        nonmatch nm; //Form for non regex matching log lines
        SortedList<string, AposNameFix> aposNameList;
        TreeNode optionsNode = null;
        Label lblStatus;    // The status label that appears in ACT's Plugin tab
        string settingsFile;
        private CheckBox varianceChkBx;
        private CheckBox nonMatchVisibleChkbx;
        private Label label1;
        private RichTextBox richTextBox1;
        SettingsSerializer xmlSettings;
        object varianceChkBxLockObject = new object();
        #endregion

        public EverQuestDPSPlugin()
        {
            InitializeComponent();
        }

        public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
            EverQuest_DPS_Plugin_Localization.EditLocalizations();
            settingsFile = Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, EverQuestDPSPluginResource.PluginSettingsFileName);
            lblStatus = pluginStatusText;   // Hand the status label's reference to our local var

            pluginScreenSpace.Controls.Add(this);
            this.Dock = DockStyle.Fill;

            int dcIndex = -1;   // Find the Data Correction node in the Options tab
            for (int i = 0; i < ActGlobals.oFormActMain.OptionsTreeView.Nodes.Count; i++)
            {
                if (ActGlobals.oFormActMain.OptionsTreeView.Nodes[i].Text == "Data Correction")
                    dcIndex = i;
            }
            if (dcIndex != -1)
            {
                // Add our own node to the Data Correction node
                optionsNode = ActGlobals.oFormActMain.OptionsTreeView.Nodes[dcIndex].Nodes.Add($"{EverQuestDPSPluginResource.pluginName} Settings");
                // Register our user control(this) to our newly create node path.  All controls added to the list will be laid out left to right, top to bottom
                ActGlobals.oFormActMain.OptionsControlSets.Add($"Data Correction\\{EverQuestDPSPluginResource.pluginName}", new List<Control> { this });
                Label lblConfig = new Label
                {
                    AutoSize = true,
                    Text = $"Settings under the {EverQuestDPSPluginResource.pluginName} tab."
                };
                pluginScreenSpace.Controls.Add(lblConfig);
            }

            aposNameList = new SortedList<string, AposNameFix>();
            xmlSettings = new SettingsSerializer(this); // Create a new settings serializer and pass it this instance
            nm = new nonmatch(this);
            LoadSettings();

            PopulateRegexArray();
            SetupEverQuestEnvironment();
            ActGlobals.oFormActMain.GetDateTimeFromLog += new FormActMain.DateTimeLogParser(ParseDateTime);
            ActGlobals.oFormActMain.BeforeLogLineRead += new LogLineEventDelegate(FormActMain_BeforeLogLineRead);
            ActGlobals.oFormActMain.UpdateCheckClicked += new FormActMain.NullDelegate(UpdateCheckClicked);

            Task updateCheckClicked = new Task(() =>
            {
                UpdateCheckClicked();
            });
            if (ActGlobals.oFormActMain.GetAutomaticUpdatesAllowed())   // If ACT is set to automatically check for updates, check for updates to the plugin
                updateCheckClicked.Start();   // If we don't put this on a separate thread, web latency will delay the plugin init phase

            ActGlobals.oFormActMain.CharacterFileNameRegex = new Regex(@"(?:.+)[\\]eqlog_(?<characterName>\S+)_(?<server>.+).txt", RegexOptions.Compiled);
            ActGlobals.oFormActMain.ZoneChangeRegex = new Regex(regexString(EverQuestDPSPluginResource.ZoneChange), RegexOptions.Compiled);
            String lblMessage = $"{EverQuestDPSPluginResource.pluginName} Plugin Started";
            changeLblStatus(lblMessage);
            if (richTextBox1.InvokeRequired)
                richTextBox1.Invoke(new Action(() =>
                {
                    richTextBox1.Text = EverQuestDPSPluginResource.LICENSE;
                }));
            else
                richTextBox1.Text = EverQuestDPSPluginResource.LICENSE;
        }

        public void DeInitPlugin()
        {
            ActGlobals.oFormActMain.GetDateTimeFromLog -= ParseDateTime;
            ActGlobals.oFormActMain.BeforeLogLineRead -= FormActMain_BeforeLogLineRead;
            ActGlobals.oFormActMain.UpdateCheckClicked -= UpdateCheckClicked;

            if (optionsNode != null)    // If we added our user control to the Options tab, remove it
            {
                optionsNode.Remove();
                ActGlobals.oFormActMain.OptionsControlSets.Remove($"Data Correction\\{EverQuestDPSPluginResource.pluginName}");
            }

            SaveSettings();
            nm.Close();
            lblStatus.Text = $"{EverQuestDPSPluginResource.pluginName} Plugin Exited";
        }

        void UpdateCheckClicked()
        {
            int.TryParse(EverQuestDPSPluginResource.pluginId, out int pluginId);
            try
            {
                Version remoteVersion = new Version(ActGlobals.oFormActMain.PluginGetRemoteVersion(pluginId));
                AssemblyFileVersionAttribute currentVersion = Assembly.GetExecutingAssembly().GetCustomAttribute(typeof(AssemblyFileVersionAttribute)) as AssemblyFileVersionAttribute;
                Version currentVersionv = new Version(currentVersion.Version);
                if (remoteVersion > currentVersionv)
                {
                    DialogResult result = MessageBox.Show($"There is an updated version of the {EverQuestDPSPluginResource.pluginName}.  Update it now?\n\n(If there is an update to ACT, you should click No and update ACT first.)", "New Version", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    switch (result)
                    {
                        case DialogResult.Yes:
                            FileInfo updatedFile = ActGlobals.oFormActMain.PluginDownload(pluginId);
                            //String githubData = ActGlobals.oFormActMain.PluginGetGithubApi(pluginId);
                            ActPluginData pluginData = ActGlobals.oFormActMain.PluginGetSelfData(this);
                            pluginData.pluginFile.Delete();
                            updatedFile.MoveTo(pluginData.pluginFile.FullName);
                            ThreadInvokes.CheckboxSetChecked(ActGlobals.oFormActMain, pluginData.cbEnabled, false);
                            Application.DoEvents();
                            ThreadInvokes.CheckboxSetChecked(ActGlobals.oFormActMain, pluginData.cbEnabled, true);
                            break;
                        case DialogResult.No:
                            break;
                        default:
                            break;

                    }
                }
            }
            catch (Exception ex)
            {
                ActGlobals.oFormActMain.WriteExceptionLog(ex, "Plugin Update Check");
            }
        }

        private DateTime ParseDateTime(String timeStamp)
        {
            DateTime.TryParseExact(timeStamp, EverQuestDPSPluginResource.eqDateTimeStampFormat, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.AssumeLocal, out DateTime currentEQTimeStamp);
            return currentEQTimeStamp;
        }

        private String regexString(String regex)
        {
            if (regex == null)
                throw new ArgumentNullException("Missing value for regex");
            else
                return $@"{EverQuestDPSPluginResource.TimeStamp} {regex}";
        }

        private void PopulateRegexArray()
        {
            String MeleeAttack = @"(?<attacker>.+) (?<attackType>" + $@"{EverQuestDPSPluginResource.attackTypes}" + @")(|s|es|bed) (?<victim>.+) for (?<damageAmount>[\d]+) (?:(?:point)(?:s|)) of damage.(?:\s\((?<damageSpecial>.+)\)){0,1}";
            String Evasion = @"(?<attacker>.*) tries to (?<attackType>\S+) (?:(?<victim>(.+)), but \1) (?:(?<evasionType>" + $@"{EverQuestDPSPluginResource.evasionTypes}" + @"))(?:\swith your staff){0,1}!(?:[\s][\(](?<evasionSpecial>.+)[\)]){0,1}";
            possesive = new Regex(EverQuestDPSPluginResource.possessiveString, RegexOptions.Compiled);
            tellsregex = new Regex(regexString(EverQuestDPSPluginResource.tellsRegex), RegexOptions.Compiled);
            selfCheck = new Regex(@"(You|you|yourself|Yourself|YOURSELF|YOU)", RegexOptions.Compiled);
            regexTupleList = new List<Tuple<Color, Regex>>();
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Clear();
            regexTupleList.Add(new Tuple<Color, Regex>(Color.Red, new Regex(regexString(MeleeAttack), RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.ForestGreen, new Regex(regexString(EverQuestDPSPluginResource.DamageShield), RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.Plum, new Regex(regexString(EverQuestDPSPluginResource.MissedMeleeAttack), RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.Goldenrod, new Regex(regexString(EverQuestDPSPluginResource.SlainMessage), RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.Red, new Regex(regexString(EverQuestDPSPluginResource.SpellDamage), RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.Maroon, new Regex(regexString(EverQuestDPSPluginResource.ZoneChange), RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.DarkBlue, new Regex(regexString(EverQuestDPSPluginResource.Heal), RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.Silver, new Regex(regexString(EverQuestDPSPluginResource.Unknown), RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.DeepSkyBlue, new Regex(regexString(Evasion), RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.LightBlue, new Regex(regexString(EverQuestDPSPluginResource.Banestrike), RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.AliceBlue, new Regex(regexString(EverQuestDPSPluginResource.SpellDamageOverTime), RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
        }

        void FormActMain_BeforeLogLineRead(bool isImport, LogLineEventArgs logInfo)
        {
            bool match = false;
            for (int i = 0; i < regexTupleList.Count; i++)
            {
                Match regexMatch = regexTupleList[i].Item2.Match(logInfo.logLine);
                if (regexMatch.Success)
                {
                    match = true;
                    logInfo.detectedType = i + 1;
                    ParseEverQuestLogLine(regexMatch, i + 1);
                    break;
                }
            }
            Match tellmatch = tellsregex.Match(logInfo.logLine);
            if (!match && !tellmatch.Success)
            {
                AddLogLineToNonMatch(logInfo.logLine);
            }
        }

        Tuple<EverQuestSwingType, String> GetTypeAndNameForPet(String nameToSetTypeTo)
        {
            Match possessiveMatch = possesive.Match(nameToSetTypeTo);
            if (possessiveMatch.Success)
            {
                switch(possessiveMatch.Groups[0].Value)
                {
                    case "pet":
                        return new Tuple<EverQuestSwingType, String>(EverQuestSwingType.Pet, nameToSetTypeTo.Substring(0, possessiveMatch.Index));
                    case "warder":
                        return new Tuple<EverQuestSwingType, String>(EverQuestSwingType.Warder, nameToSetTypeTo.Substring(0, possessiveMatch.Index));
                    case "ward":
                        return new Tuple<EverQuestSwingType, String>(EverQuestSwingType.Ward, nameToSetTypeTo.Substring(0, possessiveMatch.Index));
                    case "familiar":
                        return new Tuple<EverQuestSwingType, string>(EverQuestSwingType.Familiar, nameToSetTypeTo.Substring(0, possessiveMatch.Index));
                    default:
                        return new Tuple<EverQuestSwingType, String>(0, nameToSetTypeTo.Substring(0, possessiveMatch.Index));
                }
            }
            else
            {
                return new Tuple<EverQuestSwingType, String>(0, nameToSetTypeTo);
            }
        }

        bool CheckIfSelf(String nameOfCharacter)
        {
            Regex regexSelf = new Regex(@"((it|her|him|them)sel(f|ves))", RegexOptions.Compiled);
            return regexSelf.Match(nameOfCharacter).Success;
        }

        MasterSwing ParseMasterSwing(EverQuestSwingType attackTypeToCombine,
            Match logLineRegexMatch,
            String character1GroupName,
            String character2GroupName,
            String specialGroupName,
            DateTime dateTimeOfLogLine,
            Dnum dnumValue,
            String attackTypeGroupName,
            String damageType
            )
        {
            Tuple<EverQuestSwingType, String> attackerAndTypeMelee = GetTypeAndNameForPet(logLineRegexMatch.Groups[character1GroupName].Value);
            Tuple<EverQuestSwingType, String> victimAndTypeMelee = GetTypeAndNameForPet(logLineRegexMatch.Groups[character2GroupName].Value);
            EverQuestSwingType everQuestSwingTypeToParseMelee = 0;
            if (((attackerAndTypeMelee.Item1 & EverQuestSwingType.Pet) == EverQuestSwingType.Pet) || ((victimAndTypeMelee.Item1 & EverQuestSwingType.Pet) == EverQuestSwingType.Pet))
                everQuestSwingTypeToParseMelee = EverQuestSwingType.Pet;
            else if (((attackerAndTypeMelee.Item1 & EverQuestSwingType.Warder) == EverQuestSwingType.Warder) || ((victimAndTypeMelee.Item1 & EverQuestSwingType.Warder) == EverQuestSwingType.Warder))
                everQuestSwingTypeToParseMelee = EverQuestSwingType.Warder;
            else if (((attackerAndTypeMelee.Item1 & EverQuestSwingType.Ward) == EverQuestSwingType.Ward) || ((victimAndTypeMelee.Item1 & EverQuestSwingType.Ward) == EverQuestSwingType.Ward))
                everQuestSwingTypeToParseMelee = EverQuestSwingType.Ward;

            MasterSwing masterSwingMelee = new MasterSwing((everQuestSwingTypeToParseMelee | attackTypeToCombine).GetEverQuestSwingTypeExtensionIntValue()
                , logLineRegexMatch.Groups[specialGroupName].Success ? logLineRegexMatch.Groups[specialGroupName].Value.Contains(EverQuestDPSPluginResource.Critical) : false
                , logLineRegexMatch.Groups[specialGroupName].Success ? logLineRegexMatch.Groups[specialGroupName].Value : String.Empty
                , dnumValue
                , dateTimeOfLogLine
                , ActGlobals.oFormActMain.GlobalTimeSorter
                , logLineRegexMatch.Groups[attackTypeGroupName].Value
                , CharacterNamePersonaReplace(attackerAndTypeMelee.Item2)
                , damageType
                , CheckIfSelf(victimAndTypeMelee.Item2) ? CharacterNamePersonaReplace(attackerAndTypeMelee.Item2) : CharacterNamePersonaReplace(victimAndTypeMelee.Item2));
            masterSwingMelee.Tags.Add("lastEstimatedTime", ActGlobals.oFormActMain.LastEstimatedTime);
            return masterSwingMelee;
        }

        private void ParseEverQuestLogLine(Match regexMatch, int logMatched)
        {
            switch (logMatched)
            {
                //Melee attack
                case 1:

                    if (ActGlobals.oFormActMain.SetEncounter(ActGlobals.oFormActMain.LastKnownTime, CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value), CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value)))
                    {
                        Dnum damage = new Dnum(Int64.Parse(regexMatch.Groups["damageAmount"].Value));
                        MasterSwing masterSwingMelee = ParseMasterSwing(
                            EverQuestSwingType.Melee,
                            regexMatch,
                            "attacker",
                            "victim",
                            "damageSpecial"
                            , ParseDateTime(regexMatch.Groups["dateTimeOfLogLine"].Value),
                            damage,
                            "attackType",
                            "Hitpoints"
                        );
                        ActGlobals.oFormActMain.AddCombatAction(masterSwingMelee);
                    }
                    break;
                //Non-melee damage shield
                case 2:
                    if (ActGlobals.oFormActMain.SetEncounter(ActGlobals.oFormActMain.LastKnownTime, CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value), CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value)))
                    {
                        Dnum damage = new Dnum(Int64.Parse(regexMatch.Groups["damagePoints"].Value));
                        MasterSwing masterSwingDamageShield = ParseMasterSwing(
                                EverQuestSwingType.NonMelee,
                                regexMatch,
                                "attacker",
                                "victim",
                                "damageSpecial",
                                ParseDateTime(regexMatch.Groups["dateTimeOfLogLine"].Value),
                                damage,
                                "damageShieldDamageType",
                                "Hitpoints");
                        ActGlobals.oFormActMain.AddCombatAction(masterSwingDamageShield);
                    }
                    break;
                //Missed melee
                case 3:
                    if (ActGlobals.oFormActMain.SetEncounter(ActGlobals.oFormActMain.LastKnownTime, CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value), CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value)))
                    {
                        Dnum miss = new Dnum(Dnum.Miss);
                        MasterSwing masterSwingMissedMelee = ParseMasterSwing(
                                EverQuestSwingType.Melee,
                                regexMatch,
                                "attacker",
                                "victim",
                                "damageSpecial",
                                ParseDateTime(regexMatch.Groups["dateTimeOfLogLine"].Value),
                                miss,
                                "attackType",
                                "Miss"
                            );
                        ActGlobals.oFormActMain.AddCombatAction(masterSwingMissedMelee);
                    }
                    break;
                //Death message
                case 4:
                    MasterSwing masterSwingSlain = new MasterSwing(0, false, new Dnum(Dnum.Death), ActGlobals.oFormActMain.LastEstimatedTime, ActGlobals.oFormActMain.GlobalTimeSorter, String.Empty, CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value), String.Empty, CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value));
                    masterSwingSlain.Tags.Add("lastEstimatedTime", ActGlobals.oFormActMain.LastEstimatedTime);
                    ActGlobals.oFormActMain.AddCombatAction(masterSwingSlain);
                    break;
                //Spell Cast
                case 5:
                    if (ActGlobals.oFormActMain.SetEncounter(ActGlobals.oFormActMain.LastKnownTime, CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value), CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value)))
                    {
                        Dnum damage = new Dnum(Int64.Parse(regexMatch.Groups["damagePoints"].Value), regexMatch.Groups["typeOfDamage"].Value);
                        MasterSwing masterSwingSpellcast = ParseMasterSwing(
                                EverQuestSwingType.DirectDamageSpell,
                                regexMatch,
                                "attacker",
                                "victim",
                                "spellSpecial",
                                ParseDateTime(regexMatch.Groups["dateTimeOfLogLine"].Value),
                                damage,
                                "damageEffect",
                                "Hitpoints"
                            );
                        ActGlobals.oFormActMain.AddCombatAction(masterSwingSpellcast);
                    }
                    break;
                case 6:
                    //when checking the HistoryRecord the EndTime should be compared against default(DateTime) to determine if it an exact value among other methods such does the default(DateTime) take place before the StartTime for the HistoryRecord
                    //ActGlobals.oFormActMain.ZoneDatabaseAdd(new HistoryRecord(0, ActGlobals.oFormActMain.LastKnownTime, new DateTime(), regexMatch.Groups["zoneName"].Value != String.Empty ? regexMatch.Groups["zoneName"].Value : throw new Exception("Zone regex triggered but zone name not found."), ActGlobals.charName));
                    ActGlobals.oFormActMain.ChangeZone(regexMatch.Groups["zoneName"].Value);
                    break;
                //heal
                case 7:
                    if (ActGlobals.oFormActMain.InCombat)
                    {
                        MasterSwing masterSwingHeal = ParseMasterSwing(
                                regexMatch.Groups["overTime"].Success ? EverQuestSwingType.HealOverTime : EverQuestSwingType.InstantHealing,
                                regexMatch,
                                "healer",
                                "healingTarget",
                                "healingSpecial",
                                ParseDateTime(regexMatch.Groups["dateTimeOfLogLine"].Value),
                                new Dnum(Int64.Parse(regexMatch.Groups["healingPoints"].Value)),
                                "healingSpell",
                                "Hitpoints"
                            );
                        if (regexMatch.Groups["overHealPoints"].Success)
                            masterSwingHeal.Tags["overheal"] = Int64.Parse(regexMatch.Groups["overHealPoints"].Value);
                        ActGlobals.oFormActMain.AddCombatAction(masterSwingHeal);
                    }
                    break;
                case 8:
                    MasterSwing masterSwingUnknown = new MasterSwing(EverQuestSwingType.NonMelee.GetEverQuestSwingTypeExtensionIntValue(), false, new Dnum(Dnum.Unknown)
                    {
                        DamageString2 = regexMatch.Value
                    }, ParseDateTime(regexMatch.Groups["dateTimeOfLogLine"].Value), ActGlobals.oFormActMain.GlobalTimeSorter, "Unknown", "Unknown", "Unknown", "Unknown");
                    ActGlobals.oFormActMain.AddCombatAction(masterSwingUnknown);
                    break;
                case 9:
                    if (ActGlobals.oFormActMain.SetEncounter(ActGlobals.oFormActMain.LastKnownTime, CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value), CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value)))
                    {
                        MasterSwing masterSwingEvasion = ParseMasterSwing(
                                EverQuestSwingType.Melee,
                                regexMatch,
                                "attacker",
                                "victim",
                                "evasionSpecial",
                                ParseDateTime(regexMatch.Groups["dateTimeOfLogLine"].Value),
                                new Dnum(Dnum.NoDamage, regexMatch.Groups["evasionType"].Value),
                                "attackType",
                                "Hitpoints"
                            );
                        ActGlobals.oFormActMain.AddCombatAction(masterSwingEvasion);
                    }
                    break;
                case 10:
                    if (ActGlobals.oFormActMain.SetEncounter(ActGlobals.oFormActMain.LastKnownTime, CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value), CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value)))
                    {
                        Dnum damage = new Dnum(Int64.Parse(regexMatch.Groups["damagePoints"].Value));
                        MasterSwing masterSwingSpellcast = ParseMasterSwing(
                                EverQuestSwingType.DamageOverTimeSpell,
                                regexMatch,
                                "attacker",
                                "victim",
                                "spellSpecial",
                                ParseDateTime(regexMatch.Groups["dateTimeOfLogLine"].Value),
                                damage,
                                "damageEffect",
                                "Hitpoints"
                            );
                        ActGlobals.oFormActMain.AddCombatAction(masterSwingSpellcast);
                    }
                    break;
                default:
                    break;
            }
        }

        private string CharacterNamePersonaReplace(string PersonaString)
        {
            return selfCheck.Match(PersonaString).Success ? ActGlobals.charName : PersonaString;
        }

        void LoadSettings()
        {
            xmlSettings.AddControlSetting(varianceChkBx.Name, varianceChkBx);
            xmlSettings.AddControlSetting(nonMatchVisibleChkbx.Name, nonMatchVisibleChkbx);
            if (File.Exists(settingsFile))
            {
                using (FileStream fs = new FileStream(settingsFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (XmlTextReader xReader = new XmlTextReader(fs))
                {
                    try
                    {
                        while (xReader.Read())
                        {
                            if (xReader.NodeType == XmlNodeType.Element)
                            {
                                if (xReader.LocalName == "SettingsSerializer")
                                    xmlSettings.ImportFromXml(xReader);
                                if (xReader.LocalName == "ApostropheNameFix")
                                    LoadXmlApostropheNameFix(xReader);

                            }
                        }
                    }
                    catch (ArgumentNullException ex)
                    {
                        if (lblStatus.InvokeRequired)
                            this.lblStatus.Invoke(new Action(() => { this.lblStatus.Text = $"Argument Null for {ex.ParamName} with message: {ex.Message}"; }));
                        else
                            this.lblStatus.Text = $"Argument Null for {ex.ParamName} with message: {ex.Message}";
                    }
                    catch (Exception ex)
                    {
                        if (lblStatus.InvokeRequired)
                            this.lblStatus.Invoke(new Action(() => { this.lblStatus.Text = $"With message: {ex.Message}"; }));
                        else
                            this.lblStatus.Text = $"With message: {ex.Message}";

                    }
                }
            }
            if (varianceChkBx.InvokeRequired)
                varianceChkBx.Invoke(new Action<object, EventArgs>(VarianceChkBx_CheckedChanged));
            else
                this.populationVariance = varianceChkBx.Checked;
            nonMatchVisible_CheckedChanged(nonMatchVisibleChkbx, new EventArgs());
        }

        void SaveSettings()
        {
            using (FileStream fs = new FileStream(settingsFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                using (XmlTextWriter xWriter = new XmlTextWriter(fs, Encoding.UTF8))
                {
                    xWriter.Formatting = Formatting.Indented;
                    xWriter.Indentation = 1;
                    xWriter.IndentChar = '\t';
                    xWriter.WriteStartDocument(true);
                    xWriter.WriteStartElement("Config");    // <Config>
                    xWriter.WriteStartElement("SettingsSerializer");    // <Config><SettingsSerializer>
                    xmlSettings.ExportToXml(xWriter);   // Fill the SettingsSerializer XML
                    xWriter.WriteEndElement();  // </SettingsSerializer>
                    SaveXmlApostropheNameFix(xWriter);  // Create and fill the ApostropheNameFix node
                    xWriter.WriteEndElement();  // </Config>
                    xWriter.WriteEndDocument(); // Tie up loose ends (shouldn't be any)
                }
            }
        }

        internal class AposNameFix : IEquatable<AposNameFix>
        {
            string left, right, fullName;
            public string FullName
            {
                get
                {
                    return fullName;
                }
                set
                {
                    fullName = value;
                }
            }
            public string Right
            {
                get
                {
                    return right;
                }
                set
                {
                    right = value;
                }
            }
            public string Left
            {
                get
                {
                    return left;
                }
                set
                {
                    left = value;
                }
            }
            bool active = true;
            public bool Active
            {
                get
                {
                    return active;
                }
                set
                {
                    active = value;
                }
            }

            public AposNameFix(string FullName, string Left, string Right)
            {
                this.left = Left;
                this.right = Right;
                this.fullName = FullName;
            }
            public bool IsMatch(CombatActionEventArgs e)
            {
                if (e.attacker == left && e.theAttackType.StartsWith(right))
                    return true;
                return false;
            }
            public bool Fix(CombatActionEventArgs e)
            {
                if (!active)
                    return false;
                if (e.theAttackType == right)
                {
                    e.swingType = (int)SwingTypeEnum.Melee;
                    if (e.attacker[e.attacker.Length - 1] == 's' || e.attacker[e.attacker.Length - 1] == 'z')
                        e.attacker += "' ";
                    else
                        e.attacker += "'s ";
                    e.attacker += right;
                    e.theAttackType = e.theDamageType.ToString();
                    return true;
                }
                if (e.theAttackType.StartsWith(right + "'"))
                {
                    int trimLen = right.Length;
                    if (e.attacker[e.attacker.Length - 1] == 's' || e.attacker[e.attacker.Length - 1] == 'z')
                    {
                        e.attacker += "' ";
                        trimLen += 2;
                    }
                    else
                    {
                        e.attacker += "'s ";
                        trimLen += 3;
                    }
                    e.attacker += right;
                    e.theAttackType = e.theAttackType.Substring(trimLen);
                    return true;
                }
                return false;
            }
            public bool Equals(AposNameFix other)
            {
                return this.fullName.Equals(other.fullName);
            }
            public override string ToString()
            {
                return fullName;
            }
        }

        internal class RegexAndParseMethod
        {
            private Color parseLineColor;
            private Regex regexMatcher;
            private Action<Match> parse;

            public Color LineColor
            {
                get
                {
                    return this.parseLineColor;
                }
            }
            public Regex RegexMatcher
            {
                get { return this.regexMatcher; }
            }

            public Action<Match> Parse { get { return this.parse; } }

            public RegexAndParseMethod(Color c, Regex regex, Action<Match> action)
            {
                parseLineColor = c;
                this.regexMatcher = regex;
                this.parse = action;
            }
        }

        private int LoadXmlApostropheNameFix(XmlTextReader xReader)
        {
            int errorCount = 0;
            if (xReader.IsEmptyElement)
                return errorCount;
            while (xReader.Read())
            {
                if (xReader.NodeType == XmlNodeType.EndElement)
                    return errorCount;
                if (xReader.NodeType == XmlNodeType.Element)
                {
                    if (xReader.LocalName == "AposFix")
                    {
                        try
                        {
                            AposNameFix newItem = new AposNameFix(xReader.GetAttribute("FullName"), xReader.GetAttribute("Left"), xReader.GetAttribute("Right"))
                            {
                                Active = Boolean.Parse(xReader.GetAttribute("Active"))
                            };
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            ActGlobals.oFormActMain.WriteExceptionLog(ex, "AposFix" + xReader.ReadOuterXml());
                        }
                    }
                    else
                        break;
                }
            }
            return errorCount;
        }

        private void SaveXmlApostropheNameFix(XmlTextWriter xWriter)
        {
            xWriter.WriteStartElement("ApostropheNameFix");
            foreach (KeyValuePair<string, AposNameFix> pair in aposNameList)
            {
                xWriter.WriteStartElement("AposFix");
                xWriter.WriteAttributeString("Active", pair.Value.Active.ToString());
                xWriter.WriteAttributeString("FullName", pair.Value.FullName);
                xWriter.WriteAttributeString("Left", pair.Value.Left);
                xWriter.WriteAttributeString("Right", pair.Value.Right);
                xWriter.WriteEndElement();
            }
            xWriter.WriteEndElement();
        }

        private void SetupEverQuestEnvironment()
        {
            CultureInfo usCulture = new CultureInfo("en-US");   // This is for SQL syntax; do not change

            EncounterData.ColumnDefs.Clear();
            //Do not change the SqlDataName while doing localization
            EncounterData.ColumnDefs.Add("EncId", new EncounterData.ColumnDef("EncId", false, "CHAR(8)", "EncId", (Data) => { return string.Empty; }, (Data) => { return Data.EncId; }));
            EncounterData.ColumnDefs.Add("Title", new EncounterData.ColumnDef("Title", true, "VARCHAR(64)", "Title", (Data) => { return Data.Title; }, (Data) => { return Data.Title; }));
            EncounterData.ColumnDefs.Add("StartTime", new EncounterData.ColumnDef("StartTime", true, "TIMESTAMP", "StartTime", (Data) => { return Data.StartTime == DateTime.MaxValue ? "--:--:--" : String.Format("{0} {1}", Data.StartTime.ToShortDateString(), Data.StartTime.ToLongTimeString()); }, (Data) => { return Data.StartTime == DateTime.MaxValue ? "0000-00-00 00:00:00" : Data.StartTime.ToString("u").TrimEnd(new char[] { 'Z' }); }));
            EncounterData.ColumnDefs.Add("EndTime", new EncounterData.ColumnDef("EndTime", true, "TIMESTAMP", "EndTime", (Data) => { return Data.EndTime == DateTime.MinValue ? "--:--:--" : Data.EndTime.ToString("T"); }, (Data) => { return Data.EndTime == DateTime.MinValue ? "0000-00-00 00:00:00" : Data.EndTime.ToString("u").TrimEnd(new char[] { 'Z' }); }));
            EncounterData.ColumnDefs.Add("Duration", new EncounterData.ColumnDef("Duration", true, "INT", "Duration", (Data) => { return Data.DurationS; }, (Data) => { return Data.Duration.TotalSeconds.ToString("0"); }));
            EncounterData.ColumnDefs.Add("Damage", new EncounterData.ColumnDef("Damage", true, "BIGINT", "Damage", (Data) => { return Data.Damage.ToString(); }, (Data) => { return Data.Damage.ToString(); }));
            EncounterData.ColumnDefs.Add("EncDPS", new EncounterData.ColumnDef("EncDPS", true, "DOUBLE", "EncDPS", (Data) => { return Data.DPS.ToString(); }, (Data) => { return Data.DPS.ToString(usCulture); }));
            EncounterData.ColumnDefs.Add("Zone", new EncounterData.ColumnDef("Zone", false, "VARCHAR(64)", "Zone", (Data) => { return Data.ZoneName; }, (Data) => { return Data.ZoneName; }));
            EncounterData.ColumnDefs.Add("Kills", new EncounterData.ColumnDef("Kills", true, "INT", "Kills", (Data) => { return Data.AlliedKills.ToString(); }, (Data) => { return Data.AlliedKills.ToString(); }));
            EncounterData.ColumnDefs.Add("Deaths", new EncounterData.ColumnDef("Deaths", true, "INT", "Deaths", (Data) => { return Data.AlliedDeaths.ToString(); }, (Data) => { return Data.AlliedDeaths.ToString(); }));

            EncounterData.ExportVariables.Clear();
            EncounterData.ExportVariables.Add("n", new EncounterData.TextExportFormatter("n", "New Line", "Formatting after this element will appear on a new line.", (Data, SelectiveAllies, Extra) => { return "\n"; }));
            EncounterData.ExportVariables.Add("t", new EncounterData.TextExportFormatter("t", "Tab Character", "Formatting after this element will appear in a relative column arrangement.  (The formatting example cannot display this properly)", (Data, SelectiveAllies, Extra) => { return "\t"; }));
            EncounterData.ExportVariables.Add("title", new EncounterData.TextExportFormatter("title", "Encounter Title", "The title of the completed encounter.  This may only be used in Allies formatting.", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "title", Extra); }));
            EncounterData.ExportVariables.Add("duration", new EncounterData.TextExportFormatter("duration", "Duration", "The duration of the combatant or the duration of the encounter, displayed as mm:ss", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "duration", Extra); }));
            EncounterData.ExportVariables.Add("DURATION", new EncounterData.TextExportFormatter("DURATION", "Short Duration", "The duration of the combatant or encounter displayed in whole seconds.", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "DURATION", Extra); }));
            EncounterData.ExportVariables.Add("damage", new EncounterData.TextExportFormatter("damage", "Damage", "The amount of damage from auto-attack, spells, CAs, etc done to other combatants.", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "damage", Extra); }));
            EncounterData.ExportVariables.Add("damage-m", new EncounterData.TextExportFormatter("damage-m", "Damage M", "Damage divided by 1,000,000 (with two decimal places)", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "damage-m", Extra); }));
            EncounterData.ExportVariables.Add("damage-*", new EncounterData.TextExportFormatter("damage-*", "Damage w/suffix", "Damage divided 1/K/M/B/T/Q (with two decimal places)", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "damage-*", Extra); }));
            EncounterData.ExportVariables.Add("DAMAGE-k", new EncounterData.TextExportFormatter("DAMAGE-k", "Short Damage K", "Damage divided by 1,000 (with no decimal places)", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "DAMAGE-k", Extra); }));
            EncounterData.ExportVariables.Add("DAMAGE-m", new EncounterData.TextExportFormatter("DAMAGE-m", "Short Damage M", "Damage divided by 1,000,000 (with no decimal places)", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "DAMAGE-m", Extra); }));
            EncounterData.ExportVariables.Add("DAMAGE-b", new EncounterData.TextExportFormatter("DAMAGE-b", "Short Damage B", "Damage divided by 1,000,000,000 (with no decimal places)", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "DAMAGE-b", Extra); }));
            EncounterData.ExportVariables.Add("DAMAGE-*", new EncounterData.TextExportFormatter("DAMAGE-*", "Short Damage w/suffix", "Damage divided by 1/K/M/B/T/Q (with no decimal places)", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "DAMAGE-*", Extra); }));
            EncounterData.ExportVariables.Add("dps", new EncounterData.TextExportFormatter("dps", "DPS", "The damage total of the combatant divided by their personal duration, formatted as 12.34", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "dps", Extra); }));
            EncounterData.ExportVariables.Add("dps-*", new EncounterData.TextExportFormatter("dps-*", "DPS w/suffix", "The damage total of the combatant divided by their personal duration, formatted as 12.34K", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "dps-*", Extra); }));
            EncounterData.ExportVariables.Add("DPS", new EncounterData.TextExportFormatter("DPS", "Short DPS", "The damage total of the combatatant divided by their personal duration, formatted as 12", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "DPS", Extra); }));
            EncounterData.ExportVariables.Add("encdps", new EncounterData.TextExportFormatter("encdps", "Encounter DPS", "The damage total of the combatant divided by the duration of the encounter, formatted as 12.34 -- This is more commonly used than DPS", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "encdps", Extra); }));
            EncounterData.ExportVariables.Add("encdps-*", new EncounterData.TextExportFormatter("encdps-*", "Encounter DPS w/suffix", "The damage total of the combatant divided by the duration of the encounter, formatted as 12.34 -- This is more commonly used than DPS", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "encdps-*", Extra); }));
            EncounterData.ExportVariables.Add("ENCDPS", new EncounterData.TextExportFormatter("ENCDPS", "Short Encounter DPS", "The damage total of the combatant divided by the duration of the encounter, formatted as 12 -- This is more commonly used than DPS", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "ENCDPS", Extra); }));
            EncounterData.ExportVariables.Add("hits", new EncounterData.TextExportFormatter("hits", "Hits", "The number of attack attempts that produced damage.  IE a spell successfully doing damage.", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "hits", Extra); }));
            EncounterData.ExportVariables.Add("crithits", new EncounterData.TextExportFormatter("crithits", "Critical Hit Count", "The number of damaging attacks that were critical.", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "crithits", Extra); }));
            EncounterData.ExportVariables.Add("crithit%", new EncounterData.TextExportFormatter("crithit%", "Critical Hit Percentage", "The percentage of damaging attacks that were critical.", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "crithit%", Extra); }));
            EncounterData.ExportVariables.Add("misses", new EncounterData.TextExportFormatter("misses", "Misses", "The number of auto-attacks or CAs that produced a miss message.", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "misses", Extra); }));
            EncounterData.ExportVariables.Add("hitfailed", new EncounterData.TextExportFormatter("hitfailed", "Other Avoid", "Any type of failed attack that was not a miss.  This includes resists, reflects, blocks, dodging, etc.", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "hitfailed", Extra); }));
            EncounterData.ExportVariables.Add("swings", new EncounterData.TextExportFormatter("swings", "Swings (Attacks)", "The number of attack attempts.  This includes any auto-attacks or abilities, also including resisted abilities that do no damage.", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "swings", Extra); }));
            EncounterData.ExportVariables.Add("tohit", new EncounterData.TextExportFormatter("tohit", "To Hit %", "The percentage of hits to swings as 12.34", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "tohit", Extra); }));
            EncounterData.ExportVariables.Add("TOHIT", new EncounterData.TextExportFormatter("TOHIT", "Short To Hit %", "The percentage of hits to swings as 12", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "TOHIT", Extra); }));
            EncounterData.ExportVariables.Add("maxhit", new EncounterData.TextExportFormatter("maxhit", "Highest Hit", "The highest single damaging hit formatted as [Combatant-]SkillName-Damage#", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "maxhit", Extra); }));
            EncounterData.ExportVariables.Add("MAXHIT", new EncounterData.TextExportFormatter("MAXHIT", "Short Highest Hit", "The highest single damaging hit formatted as [Combatant-]Damage#", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "MAXHIT", Extra); }));
            EncounterData.ExportVariables.Add("maxhit-*", new EncounterData.TextExportFormatter("maxhit-*", "Highest Hit w/ suffix", "MaxHit divided by 1/K/M/B/T/Q", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "maxhit-*", Extra); }));
            EncounterData.ExportVariables.Add("MAXHIT-*", new EncounterData.TextExportFormatter("MAXHIT-*", "Short Highest Hit w/ suffix", "Short MaxHit divided by 1/K/M/B/T/Q", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "MAXHIT-*", Extra); }));
            EncounterData.ExportVariables.Add("healed", new EncounterData.TextExportFormatter("healed", "Healed", "The numerical total of all heals, wards or similar sourced from this combatant.", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "healed", Extra); }));
            //EncounterData.ExportVariables.Add("enchps", new EncounterData.TextExportFormatter("enchps", "Encounter HPS", "The healing total of the combatant divided by the duration of the encounter, formatted as 12.34", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "enchps", Extra); }));
            //EncounterData.ExportVariables.Add("enchps-*", new EncounterData.TextExportFormatter("enchps-*", "Encounter HPS w/suffix", "Encounter HPS divided by 1/K/M/B/T/Q", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "enchps-*", Extra); }));
            //EncounterData.ExportVariables.Add("ENCHPS", new EncounterData.TextExportFormatter("ENCHPS", "Short Encounter HPS", "The healing total of the combatant divided by the duration of the encounter, formatted as 12", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "ENCHPS", Extra); }));
            EncounterData.ExportVariables.Add("heals", new EncounterData.TextExportFormatter("heals", "Heal Count", "The total number of heals from this combatant.", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "heals", Extra); }));
            EncounterData.ExportVariables.Add("critheals", new EncounterData.TextExportFormatter("critheals", "Critical Heal Count", "The number of heals that were critical.", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "critheals", Extra); }));
            EncounterData.ExportVariables.Add("critheal%", new EncounterData.TextExportFormatter("critheal%", "Critical Heal Percentage", "The percentage of heals that were critical.", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "critheal%", Extra); }));
            EncounterData.ExportVariables.Add("cures", new EncounterData.TextExportFormatter("cures", "Cure or Dispel Count", "The total number of times the combatant cured or dispelled", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "cures", Extra); }));
            EncounterData.ExportVariables.Add("maxheal", new EncounterData.TextExportFormatter("maxheal", "Highest Heal", "The highest single healing amount formatted as [Combatant-]SkillName-Healing#", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "maxheal", Extra); }));
            EncounterData.ExportVariables.Add("MAXHEAL", new EncounterData.TextExportFormatter("MAXHEAL", "Short Highest Heal", "The highest single healing amount formatted as [Combatant-]Healing#", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "MAXHEAL", Extra); }));
            EncounterData.ExportVariables.Add("maxhealward", new EncounterData.TextExportFormatter("maxhealward", "Highest Heal/Ward", "The highest single healing/warding amount formatted as [Combatant-]SkillName-Healing#", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "maxhealward", Extra); }));
            EncounterData.ExportVariables.Add("MAXHEALWARD", new EncounterData.TextExportFormatter("MAXHEALWARD", "Short Highest Heal/Ward", "The highest single healing/warding amount formatted as [Combatant-]Healing#", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "MAXHEALWARD", Extra); }));
            EncounterData.ExportVariables.Add("maxheal-*", new EncounterData.TextExportFormatter("maxheal-*", "Highest Heal w/ suffix", "Highest Heal divided by 1/K/M/B/T/Q", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "maxheal-*", Extra); }));
            EncounterData.ExportVariables.Add("MAXHEAL-*", new EncounterData.TextExportFormatter("MAXHEAL-*", "Short Highest Heal w/ suffix", "Short Highest Heal divided by 1/K/M/B/T/Q", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "MAXHEAL-*", Extra); }));
            EncounterData.ExportVariables.Add("maxhealward-*", new EncounterData.TextExportFormatter("maxhealward-*", "Highest Heal/Ward w/ suffix", "Highest Heal/Ward divided by 1/K/M/B/T/Q", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "maxhealward-*", Extra); }));
            EncounterData.ExportVariables.Add("MAXHEALWARD-*", new EncounterData.TextExportFormatter("MAXHEALWARD-*", "Short Highest Heal/Ward w/ suffix", "Short Highest Heal/Ward divided by 1/K/M/B/T/Q", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "MAXHEALWARD-*", Extra); }));
            EncounterData.ExportVariables.Add("damagetaken", new EncounterData.TextExportFormatter("damagetaken", "Damage Received", "The total amount of damage this combatant received.", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "damagetaken", Extra); }));
            EncounterData.ExportVariables.Add("damagetaken-*", new EncounterData.TextExportFormatter("damagetaken-*", "Damage Received w/suffix", "Damage Received divided by 1/K/M/B/T/Q", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "damagetaken-*", Extra); }));
            EncounterData.ExportVariables.Add("healstaken", new EncounterData.TextExportFormatter("healstaken", "Healing Received", "The total amount of healing this combatant received.", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "healstaken", Extra); }));
            EncounterData.ExportVariables.Add("healstaken-*", new EncounterData.TextExportFormatter("healstaken-*", "Healing Received w/suffix", "Healing Received divided by 1/K/M/B/T/Q", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "healstaken-*", Extra); }));
            EncounterData.ExportVariables.Add("kills", new EncounterData.TextExportFormatter("kills", "Killing Blows", "The total number of times this character landed a killing blow.", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "kills", Extra); }));
            EncounterData.ExportVariables.Add("deaths", new EncounterData.TextExportFormatter("deaths", "Deaths", "The total number of times this character was killed by another.", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "deaths", Extra); }));

            CombatantData.ColumnDefs.Clear();
            CombatantData.ColumnDefs.Add("EncId", new CombatantData.ColumnDef("EncId", false, "CHAR(8)", "EncId", (Data) => { return string.Empty; }, (Data) => { return Data.Parent.EncId; }, (Left, Right) => { return 0; }));
            CombatantData.ColumnDefs.Add("Ally", new CombatantData.ColumnDef("Ally", false, "CHAR(1)", "Ally", (Data) => { return Data.Parent.GetAllies().Contains(Data).ToString(); }, (Data) => { return Data.Parent.GetAllies().Contains(Data) ? "T" : "F"; }, (Left, Right) => { return Left.Parent.GetAllies().Contains(Left).CompareTo(Right.Parent.GetAllies().Contains(Right)); }));
            CombatantData.ColumnDefs.Add("Name", new CombatantData.ColumnDef("Name", true, "VARCHAR(64)", "Name", (Data) => { return Data.Name; }, (Data) => { return Data.Name; }, (Left, Right) => { return Left.Name.CompareTo(Right.Name); }));
            CombatantData.ColumnDefs.Add("StartTime", new CombatantData.ColumnDef("StartTime", true, "TIMESTAMP", "StartTime", (Data) => { return Data.StartTime == DateTime.MaxValue ? "--:--:--" : Data.StartTime.ToString("T"); }, (Data) => { return Data.StartTime == DateTime.MaxValue ? "0000-00-00 00:00:00" : Data.StartTime.ToString("u").TrimEnd(new char[] { 'Z' }); }, (Left, Right) => { return Left.StartTime.CompareTo(Right.StartTime); }));
            CombatantData.ColumnDefs.Add("EndTime", new CombatantData.ColumnDef("EndTime", false, "TIMESTAMP", "EndTime", (Data) => { return Data.EndTime == DateTime.MinValue ? "--:--:--" : Data.EndTime.ToString("T"); }, (Data) => { return Data.EndTime == DateTime.MinValue ? "0000-00-00 00:00:00" : Data.EndTime.ToString("u").TrimEnd(new char[] { 'Z' }); }, (Left, Right) => { return Left.EndTime.CompareTo(Right.EndTime); }));
            CombatantData.ColumnDefs.Add("Duration", new CombatantData.ColumnDef("Duration", true, "INT", "Duration", (Data) => { return Data.DurationS; }, (Data) => { return Data.Duration.TotalSeconds.ToString("0"); }, (Left, Right) => { return Left.Duration.CompareTo(Right.Duration); }));
            CombatantData.ColumnDefs.Add("Damage", new CombatantData.ColumnDef("Damage", true, "BIGINT", "Damage", (Data) => { return Data.Damage.ToString(); }, (Data) => { return Data.Damage.ToString(); }, (Left, Right) => { return Left.Damage.CompareTo(Right.Damage); }));
            CombatantData.ColumnDefs.Add("Damage%", new CombatantData.ColumnDef("Damage%", true, "VARCHAR(4)", "DamagePerc", (Data) => { return Data.DamagePercent; }, (Data) => { return Data.DamagePercent; }, (Left, Right) => { return Left.Damage.CompareTo(Right.Damage); }));
            CombatantData.ColumnDefs.Add("Kills", new CombatantData.ColumnDef("Kills", false, "INT", "Kills", (Data) => { return Data.Kills.ToString(); }, (Data) => { return Data.Kills.ToString(); }, (Left, Right) => { return Left.Kills.CompareTo(Right.Kills); }));
            CombatantData.ColumnDefs.Add("Healed", new CombatantData.ColumnDef("Healed", false, "BIGINT", "Healed", (Data) => { return Data.Healed.ToString(); }, (Data) => { return Data.Healed.ToString(); }, (Left, Right) => { return Left.Healed.CompareTo(Right.Healed); }));
            CombatantData.ColumnDefs.Add("Healed%", new CombatantData.ColumnDef("Healed%", false, "VARCHAR(4)", "HealedPerc", (Data) => { return Data.HealedPercent; }, (Data) => { return Data.HealedPercent; }, (Left, Right) => { return Left.Healed.CompareTo(Right.Healed); }));
            CombatantData.ColumnDefs.Add("CritHeals", new CombatantData.ColumnDef("CritHeals", false, "INT", "CritHeals", (Data) => { return Data.CritHeals.ToString(); }, (Data) => { return Data.CritHeals.ToString(); }, (Left, Right) => { return Left.CritHeals.CompareTo(Right.CritHeals); }));
            CombatantData.ColumnDefs.Add("Heals", new CombatantData.ColumnDef("Heals", false, "INT", "Heals", (Data) => { return Data.Heals.ToString(); }, (Data) => { return Data.Heals.ToString(); }, (Left, Right) => { return Left.Heals.CompareTo(Right.Heals); }));
            CombatantData.ColumnDefs.Add("Cures", new CombatantData.ColumnDef("Cures", false, "INT", "CureDispels", (Data) => { return Data.CureDispels.ToString(); }, (Data) => { return Data.CureDispels.ToString(); }, (Left, Right) => { return Left.CureDispels.CompareTo(Right.CureDispels); }));
            //CombatantData.ColumnDefs.Add("PowerDrain", new CombatantData.ColumnDef("PowerDrain", true, "BIGINT", "PowerDrain", (Data) => { return Data.PowerDamage.ToString(GetIntCommas()); }, (Data) => { return Data.PowerDamage.ToString(); }, (Left, Right) => { return Left.PowerDamage.CompareTo(Right.PowerDamage); }));
            //CombatantData.ColumnDefs.Add("PowerReplenish", new CombatantData.ColumnDef("PowerReplenish", false, "BIGINT", "PowerReplenish", (Data) => { return Data.PowerReplenish.ToString(GetIntCommas()); }, (Data) => { return Data.PowerReplenish.ToString(); }, (Left, Right) => { return Left.PowerReplenish.CompareTo(Right.PowerReplenish); }));
            CombatantData.ColumnDefs.Add("DPS", new CombatantData.ColumnDef("DPS", false, "DOUBLE", "DPS", (Data) => { return Data.DPS.ToString(); }, (Data) => { return Data.DPS.ToString(usCulture); }, (Left, Right) => { return Left.DPS.CompareTo(Right.DPS); }));
            CombatantData.ColumnDefs.Add("EncDPS", new CombatantData.ColumnDef("EncDPS", true, "DOUBLE", "EncDPS", (Data) => { return Data.EncDPS.ToString(); }, (Data) => { return Data.EncDPS.ToString(usCulture); }, (Left, Right) => { return Left.Damage.CompareTo(Right.Damage); }));
            //CombatantData.ColumnDefs.Add("EncHPS", new CombatantData.ColumnDef("EncHPS", true, "DOUBLE", "EncHPS", (Data) => { return Data.EncHPS.ToString(); }, (Data) => { return Data.EncHPS.ToString(usCulture); }, (Left, Right) => { return Left.Healed.CompareTo(Right.Healed); }));
            CombatantData.ColumnDefs.Add("Hits", new CombatantData.ColumnDef("Hits", false, "INT", "Hits", (Data) => { return Data.Hits.ToString(); }, (Data) => { return Data.Hits.ToString(); }, (Left, Right) => { return Left.Hits.CompareTo(Right.Hits); }));
            CombatantData.ColumnDefs.Add("CritHits", new CombatantData.ColumnDef("CritHits", false, "INT", "CritHits", (Data) => { return Data.CritHits.ToString(); }, (Data) => { return Data.CritHits.ToString(); }, (Left, Right) => { return Left.CritHits.CompareTo(Right.CritHits); }));
            CombatantData.ColumnDefs.Add("Avoids", new CombatantData.ColumnDef("Avoids", false, "INT", "Blocked", (Data) => { return Data.Blocked.ToString(); }, (Data) => { return Data.Blocked.ToString(); }, (Left, Right) => { return Left.Blocked.CompareTo(Right.Blocked); }));
            CombatantData.ColumnDefs.Add("Misses", new CombatantData.ColumnDef("Misses", false, "INT", "Misses", (Data) => { return Data.Misses.ToString(); }, (Data) => { return Data.Misses.ToString(); }, (Left, Right) => { return Left.Misses.CompareTo(Right.Misses); }));
            CombatantData.ColumnDefs.Add("Swings", new CombatantData.ColumnDef("Swings", false, "INT", "Swings", (Data) => { return Data.Swings.ToString(); }, (Data) => { return Data.Swings.ToString(); }, (Left, Right) => { return Left.Swings.CompareTo(Right.Swings); }));
            CombatantData.ColumnDefs.Add("HealingTaken", new CombatantData.ColumnDef("HealingTaken", false, "BIGINT", "HealsTaken", (Data) => { return Data.HealsTaken.ToString(); }, (Data) => { return Data.HealsTaken.ToString(); }, (Left, Right) => { return Left.HealsTaken.CompareTo(Right.HealsTaken); }));
            CombatantData.ColumnDefs.Add("DamageTaken", new CombatantData.ColumnDef("DamageTaken", true, "BIGINT", "DamageTaken", (Data) => { return Data.DamageTaken.ToString(); }, (Data) => { return Data.DamageTaken.ToString(); }, (Left, Right) => { return Left.DamageTaken.CompareTo(Right.DamageTaken); }));
            CombatantData.ColumnDefs.Add("Deaths", new CombatantData.ColumnDef("Deaths", true, "INT", "Deaths", (Data) => { return Data.Deaths.ToString(); }, (Data) => { return Data.Deaths.ToString(); }, (Left, Right) => { return Left.Deaths.CompareTo(Right.Deaths); }));
            CombatantData.ColumnDefs.Add("ToHit%", new CombatantData.ColumnDef("ToHit%", false, "FLOAT", "ToHit", (Data) => { return Data.ToHit.ToString(); }, (Data) => { return Data.ToHit.ToString(usCulture); }, (Left, Right) => { return Left.ToHit.CompareTo(Right.ToHit); }));
            CombatantData.ColumnDefs.Add("CritDam%", new CombatantData.ColumnDef("CritDam%", false, "VARCHAR(8)", "CritDamPerc", (Data) => { return Data.CritDamPerc.ToString("0'%"); }, (Data) => { return Data.CritDamPerc.ToString("0'%"); }, (Left, Right) => { return Left.CritDamPerc.CompareTo(Right.CritDamPerc); }));
            CombatantData.ColumnDefs.Add("CritHeal%", new CombatantData.ColumnDef("CritHeal%", false, "VARCHAR(8)", "CritHealPerc", (Data) => { return Data.CritHealPerc.ToString("0'%"); }, (Data) => { return Data.CritHealPerc.ToString("0'%"); }, (Left, Right) => { return Left.CritHealPerc.CompareTo(Right.CritHealPerc); }));
            CombatantData.ColumnDefs.Add("CritTypes", new CombatantData.ColumnDef("CritTypes", true, "VARCHAR(32)", "CritTypes", CombatantDataGetCritTypes, CombatantDataGetCritTypes, (Left, Right) => { return CombatantDataGetCritTypes(Left).CompareTo(CombatantDataGetCritTypes(Right)); }));

            CombatantData.ColumnDefs["Damage"].GetCellForeColor = (Data) => { return Color.DarkRed; };
            CombatantData.ColumnDefs["Damage%"].GetCellForeColor = (Data) => { return Color.DarkRed; };
            CombatantData.ColumnDefs["Healed"].GetCellForeColor = (Data) => { return Color.DarkBlue; };
            CombatantData.ColumnDefs["Healed%"].GetCellForeColor = (Data) => { return Color.DarkBlue; };
            CombatantData.ColumnDefs["DPS"].GetCellForeColor = (Data) => { return Color.DarkRed; };
            CombatantData.ColumnDefs["DamageTaken"].GetCellForeColor = (Data) => { return Color.DarkOrange; };

            CombatantData.OutgoingDamageTypeDataObjects = new Dictionary<string, CombatantData.DamageTypeDef>
        {
            {"Auto-Attack (Out)", new CombatantData.DamageTypeDef("Auto-Attack (Out)", -1, Color.DarkGoldenrod)},
            {"Skill/Ability (Out)", new CombatantData.DamageTypeDef("Skill/Ability (Out)", -1, Color.DarkOrange)},
            {"Outgoing Damage", new CombatantData.DamageTypeDef("Outgoing Damage", 0, Color.Orange)},
            {"Direct Damage Spell (Out)", new CombatantData.DamageTypeDef("Direct Damage Spell (Out)", -1, Color.LightCyan) },
            {"Damage Over Time Spell (Out)", new CombatantData.DamageTypeDef("Damage Over Time Spell (Out)", -1, Color.ForestGreen) },
            {"Bane (Out)", new CombatantData.DamageTypeDef("Bane (Out)", -1, Color.LightGreen) },
            {"Instant Healed (Out)", new CombatantData.DamageTypeDef("Instant Healed (Out)", 1, Color.Blue)},
            {"Heal Over Time (Out)", new CombatantData.DamageTypeDef("Heal Over Time (Out)", 1, Color.DarkBlue)},
                {"Pet Melee (Out)", new CombatantData.DamageTypeDef("Pet Melee (Out)", -1, Color.GreenYellow)},
                {"Warder Melee (Out)", new CombatantData.DamageTypeDef("Warder Melee (Out)", -1, Color.Aqua)},
            {"All Outgoing (Ref)", new CombatantData.DamageTypeDef("All Outgoing (Ref)", 0, Color.Black)}
        };
            CombatantData.IncomingDamageTypeDataObjects = new Dictionary<string, CombatantData.DamageTypeDef>
        {
            {"Incoming Damage", new CombatantData.DamageTypeDef("Incoming Damage", -1, Color.Red)},
            {"Incoming NonMelee Damage", new CombatantData.DamageTypeDef("Incoming NonMelee Damage", -1 , Color.DarkRed) },
            {"Direct Damage Spell (Inc)", new CombatantData.DamageTypeDef("Direct Damage Spell (Inc)", -1, Color.LightCyan) },
            {"Damage Over Time Spell (Inc)", new CombatantData.DamageTypeDef("Damage Over Time Spell (Inc)", -1, Color.Orchid) },
            {"Instant Healed (Inc)",new CombatantData.DamageTypeDef("Instant Healed (Inc)", 1, Color.LimeGreen)},
            {"Heal Over Time (Inc)",new CombatantData.DamageTypeDef("Heal Over Time (Inc)", 1, Color.DarkGreen)},
            {"Pet Melee (Inc)", new CombatantData.DamageTypeDef("Pet Melee (Inc)", -1, Color.Green)},
                {"Warder Melee (Inc)", new CombatantData.DamageTypeDef("Warder Melee (Inc)", -1, Color.Lavender)},
            {"All Incoming (Ref)",new CombatantData.DamageTypeDef("All Incoming (Ref)", 0, Color.Black)}
        };
            CombatantData.SwingTypeToDamageTypeDataLinksOutgoing = new SortedDictionary<int, List<string>>
        {
            {EverQuestSwingType.Melee.GetEverQuestSwingTypeExtensionIntValue(), new List<string> { "Auto-Attack (Out)", "Outgoing Damage" } },
            {EverQuestSwingType.NonMelee.GetEverQuestSwingTypeExtensionIntValue(), new List<string> { "Skill/Ability (Out)", "Outgoing Damage" } },
            {EverQuestSwingType.DirectDamageSpell.GetEverQuestSwingTypeExtensionIntValue(), new List<string> { "Direct Damage Spell (Out)" , "Outgoing Damage"} },
            {EverQuestSwingType.DamageOverTimeSpell.GetEverQuestSwingTypeExtensionIntValue(), new List<string>{"Damage Over Time Spell (Out)", "Outgoing Damage"} },
            {EverQuestSwingType.InstantHealing.GetEverQuestSwingTypeExtensionIntValue(), new List<string> { "Instant Healed (Out)" } },
            {EverQuestSwingType.HealOverTime.GetEverQuestSwingTypeExtensionIntValue(), new List<string> { "Heal Over Time (Out)" } },
            {EverQuestSwingType.PetMelee.GetEverQuestSwingTypeExtensionIntValue(), new List<string> { "Pet Melee (Out)" } },
            {EverQuestSwingType.WarderMelee.GetEverQuestSwingTypeExtensionIntValue(), new List<string> { "Warder Melee (Out)" } }
        };
            CombatantData.SwingTypeToDamageTypeDataLinksIncoming = new SortedDictionary<int, List<string>>
        {
            {EverQuestSwingType.Melee.GetEverQuestSwingTypeExtensionIntValue(), new List<string> { "Incoming Damage" } },
            {EverQuestSwingType.NonMelee.GetEverQuestSwingTypeExtensionIntValue(), new List<string> { "Incoming NonMelee Damage" } },
            {EverQuestSwingType.DirectDamageSpell.GetEverQuestSwingTypeExtensionIntValue(), new List<string> { "Direct Damage Spell (Inc)" } },
            {EverQuestSwingType.InstantHealing.GetEverQuestSwingTypeExtensionIntValue(), new List<string> { "Instant Healed (Inc)" } },
            {EverQuestSwingType.DamageOverTimeSpell.GetEverQuestSwingTypeExtensionIntValue(), new List<string> {"Damage Over Time Spell (Inc)"} },
            {EverQuestSwingType.HealOverTime.GetEverQuestSwingTypeExtensionIntValue(), new List<string> { "Heal Over Time (Inc)" } },
            {EverQuestSwingType.PetMelee.GetEverQuestSwingTypeExtensionIntValue(), new List<string> { "Pet Melee (Inc)" } },
            {EverQuestSwingType.WarderMelee.GetEverQuestSwingTypeExtensionIntValue(), new List<string> { "Warder Melee (Inc)" } }
        };

            CombatantData.DamageSwingTypes = new List<int> {
                EverQuestSwingType.Melee.GetEverQuestSwingTypeExtensionIntValue(),
                EverQuestSwingType.NonMelee.GetEverQuestSwingTypeExtensionIntValue(),
                EverQuestSwingType.DirectDamageSpell.GetEverQuestSwingTypeExtensionIntValue(),
                EverQuestSwingType.DamageOverTimeSpell.GetEverQuestSwingTypeExtensionIntValue(),
                EverQuestSwingType.Bane.GetEverQuestSwingTypeExtensionIntValue(),
                EverQuestSwingType.PetMelee.GetEverQuestSwingTypeExtensionIntValue(),
                EverQuestSwingType.WarderMelee.GetEverQuestSwingTypeExtensionIntValue(),
            };
            CombatantData.HealingSwingTypes = new List<int> { EverQuestSwingType.InstantHealing.GetEverQuestSwingTypeExtensionIntValue(), EverQuestSwingType.HealOverTime.GetEverQuestSwingTypeExtensionIntValue() };

            CombatantData.ExportVariables.Clear();
            CombatantData.ExportVariables.Add("n", new CombatantData.TextExportFormatter("n", "New Line", "Formatting after this element will appear on a new line.", (Data, Extra) => { return "\n"; }));
            CombatantData.ExportVariables.Add("t", new CombatantData.TextExportFormatter("t", "Tab Character", "Formatting after this element will appear in a relative column arrangement.  (The formatting example cannot display this properly)", (Data, Extra) => { return "\t"; }));

            DamageTypeData.ColumnDefs.Clear();
            DamageTypeData.ColumnDefs.Add("EncId", new DamageTypeData.ColumnDef("EncId", false, "CHAR(8)", "EncId", (Data) => { return string.Empty; }, (Data) => { return Data.Parent.Parent.EncId; }));
            DamageTypeData.ColumnDefs.Add("Combatant", new DamageTypeData.ColumnDef("Combatant", false, "VARCHAR(64)", "Combatant", (Data) => { return Data.Parent.Name; }, (Data) => { return Data.Parent.Name; }));
            DamageTypeData.ColumnDefs.Add("Type", new DamageTypeData.ColumnDef("Type", true, "VARCHAR(64)", "Type", (Data) => { return Data.Type; }, (Data) => { return Data.Type; }));
            DamageTypeData.ColumnDefs.Add("StartTime", new DamageTypeData.ColumnDef("StartTime", false, "TIMESTAMP", "StartTime", (Data) => { return Data.StartTime == DateTime.MaxValue ? "--:--:--" : Data.StartTime.ToString("T"); }, (Data) => { return Data.StartTime == DateTime.MaxValue ? "0000-00-00 00:00:00" : Data.StartTime.ToString("u").TrimEnd(new char[] { 'Z' }); }));
            DamageTypeData.ColumnDefs.Add("EndTime", new DamageTypeData.ColumnDef("EndTime", false, "TIMESTAMP", "EndTime", (Data) => { return Data.EndTime == DateTime.MinValue ? "--:--:--" : Data.EndTime.ToString("T"); }, (Data) => { return Data.EndTime == DateTime.MinValue ? "0000-00-00 00:00:00" : Data.EndTime.ToString("u").TrimEnd(new char[] { 'Z' }); }));
            DamageTypeData.ColumnDefs.Add("Duration", new DamageTypeData.ColumnDef("Duration", false, "INT", "Duration", (Data) => { return Data.DurationS; }, (Data) => { return Data.Duration.TotalSeconds.ToString("0"); }));
            DamageTypeData.ColumnDefs.Add("Damage", new DamageTypeData.ColumnDef("Damage", true, "BIGINT", "Damage", (Data) => { return Data.Damage.ToString(); }, (Data) => { return Data.Damage.ToString(); }));
            DamageTypeData.ColumnDefs.Add("EncDPS", new DamageTypeData.ColumnDef("EncDPS", true, "DOUBLE", "EncDPS", (Data) => { return Data.EncDPS.ToString(); }, (Data) => { return Data.EncDPS.ToString(usCulture); }));
            DamageTypeData.ColumnDefs.Add("CharDPS", new DamageTypeData.ColumnDef("CharDPS", false, "DOUBLE", "CharDPS", (Data) => { return Data.CharDPS.ToString(); }, (Data) => { return Data.CharDPS.ToString(usCulture); }));
            DamageTypeData.ColumnDefs.Add("DPS", new DamageTypeData.ColumnDef("DPS", false, "DOUBLE", "DPS", (Data) => { return Data.DPS.ToString(); }, (Data) => { return Data.DPS.ToString(usCulture); }));
            DamageTypeData.ColumnDefs.Add("Average", new DamageTypeData.ColumnDef("Average", true, "DOUBLE", "Average", (Data) => { return Data.Average.ToString(); }, (Data) => { return Data.Average.ToString(usCulture); }));
            DamageTypeData.ColumnDefs.Add("Median", new DamageTypeData.ColumnDef("Median", false, "BIGINT", "Median", (Data) => { return Data.Median.ToString(); }, (Data) => { return Data.Median.ToString(); }));
            DamageTypeData.ColumnDefs.Add("MinHit", new DamageTypeData.ColumnDef("MinHit", true, "BIGINT", "MinHit", (Data) => { return Data.MinHit.ToString(); }, (Data) => { return Data.MinHit.ToString(); }));
            DamageTypeData.ColumnDefs.Add("MaxHit", new DamageTypeData.ColumnDef("MaxHit", true, "BIGINT", "MaxHit", (Data) => { return Data.MaxHit.ToString(); }, (Data) => { return Data.MaxHit.ToString(); }));
            DamageTypeData.ColumnDefs.Add("Hits", new DamageTypeData.ColumnDef("Hits", true, "INT", "Hits", (Data) => { return Data.Hits.ToString(); }, (Data) => { return Data.Hits.ToString(); }));
            DamageTypeData.ColumnDefs.Add("CritHits", new DamageTypeData.ColumnDef("CritHits", false, "INT", "CritHits", (Data) => { return Data.CritHits.ToString(); }, (Data) => { return Data.CritHits.ToString(); }));
            DamageTypeData.ColumnDefs.Add("Avoids", new DamageTypeData.ColumnDef("Avoids", false, "INT", "Blocked", (Data) => { return Data.Blocked.ToString(); }, (Data) => { return Data.Blocked.ToString(); }));
            DamageTypeData.ColumnDefs.Add("Misses", new DamageTypeData.ColumnDef("Misses", false, "INT", "Misses", (Data) => { return Data.Misses.ToString(); }, (Data) => { return Data.Misses.ToString(); }));
            DamageTypeData.ColumnDefs.Add("Swings", new DamageTypeData.ColumnDef("Swings", true, "INT", "Swings", (Data) => { return Data.Swings.ToString(); }, (Data) => { return Data.Swings.ToString(); }));
            DamageTypeData.ColumnDefs.Add("ToHit", new DamageTypeData.ColumnDef("ToHit", false, "FLOAT", "ToHit", (Data) => { return Data.ToHit.ToString(); }, (Data) => { return Data.ToHit.ToString(); }));
            DamageTypeData.ColumnDefs.Add("AvgDelay", new DamageTypeData.ColumnDef("AvgDelay", false, "FLOAT", "AverageDelay", (Data) => { return Data.AverageDelay.ToString(); }, (Data) => { return Data.AverageDelay.ToString(usCulture); }));
            DamageTypeData.ColumnDefs.Add("Crit%", new DamageTypeData.ColumnDef("Crit%", false, "VARCHAR(8)", "CritPerc", (Data) => { return Data.CritPerc.ToString("0'%"); }, (Data) => { return Data.CritPerc.ToString("0'%"); }));
            DamageTypeData.ColumnDefs.Add("CritTypes", new DamageTypeData.ColumnDef("CritTypes", true, "VARCHAR(32)", "CritTypes", DamageTypeDataGetCritTypes, DamageTypeDataGetCritTypes));


            AttackType.ColumnDefs.Clear();
            AttackType.ColumnDefs.Add("EncId", new AttackType.ColumnDef("EncId", false, "CHAR(8)", "EncId", (Data) => { return string.Empty; }, (Data) => { return Data.Parent.Parent.Parent.EncId; }, (Left, Right) => { return 0; }));
            AttackType.ColumnDefs.Add("Attacker", new AttackType.ColumnDef("Attacker", false, "VARCHAR(64)", "Attacker", (Data) => { return Data.Parent.Outgoing ? Data.Parent.Parent.Name : string.Empty; }, (Data) => { return Data.Parent.Outgoing ? Data.Parent.Parent.Name : string.Empty; }, (Left, Right) => { return 0; }));
            AttackType.ColumnDefs.Add("Victim", new AttackType.ColumnDef("Victim", false, "VARCHAR(64)", "Victim", (Data) => { return Data.Parent.Outgoing ? string.Empty : Data.Parent.Parent.Name; }, (Data) => { return Data.Parent.Outgoing ? string.Empty : Data.Parent.Parent.Name; }, (Left, Right) => { return 0; }));
            AttackType.ColumnDefs.Add("SwingType", new AttackType.ColumnDef("SwingType", false, "TINYINT", "SwingType", GetAttackTypeSwingType, GetAttackTypeSwingType, (Left, Right) => { return GetAttackTypeSwingType(Left).CompareTo(GetAttackTypeSwingType(Right)); }));
            AttackType.ColumnDefs.Add("Type", new AttackType.ColumnDef("Type", true, "VARCHAR(64)", "Type", (Data) => { return Data.Type; }, (Data) => { return Data.Type; }, (Left, Right) => { return Left.Type.CompareTo(Right.Type); }));
            AttackType.ColumnDefs.Add("StartTime", new AttackType.ColumnDef("StartTime", false, "TIMESTAMP", "StartTime", (Data) => { return Data.StartTime == DateTime.MaxValue ? "--:--:--" : Data.StartTime.ToString("T"); }, (Data) => { return Data.StartTime == DateTime.MaxValue ? "0000-00-00 00:00:00" : Data.StartTime.ToString("u").TrimEnd(new char[] { 'Z' }); }, (Left, Right) => { return Left.StartTime.CompareTo(Right.StartTime); }));
            AttackType.ColumnDefs.Add("EndTime", new AttackType.ColumnDef("EndTime", false, "TIMESTAMP", "EndTime", (Data) => { return Data.EndTime == DateTime.MinValue ? "--:--:--" : Data.EndTime.ToString("T"); }, (Data) => { return Data.EndTime == DateTime.MinValue ? "0000-00-00 00:00:00" : Data.EndTime.ToString("u").TrimEnd(new char[] { 'Z' }); }, (Left, Right) => { return Left.EndTime.CompareTo(Right.EndTime); }));
            AttackType.ColumnDefs.Add("Duration", new AttackType.ColumnDef("Duration", false, "INT", "Duration", (Data) => { return Data.DurationS; }, (Data) => { return Data.Duration.TotalSeconds.ToString("0"); }, (Left, Right) => { return Left.Duration.CompareTo(Right.Duration); }));
            AttackType.ColumnDefs.Add("Damage", new AttackType.ColumnDef("Damage", true, "BIGINT", "Damage", (Data) => { return Data.Damage.ToString(); }, (Data) => { return Data.Damage.ToString(); }, (Left, Right) => { return Left.Damage.CompareTo(Right.Damage); }));
            AttackType.ColumnDefs.Add("EncDPS", new AttackType.ColumnDef("EncDPS", true, "DOUBLE", "EncDPS", (Data) => { return Data.EncDPS.ToString(); }, (Data) => { return Data.EncDPS.ToString(usCulture); }, (Left, Right) => { return Left.EncDPS.CompareTo(Right.EncDPS); }));
            AttackType.ColumnDefs.Add("CharDPS", new AttackType.ColumnDef("CharDPS", false, "DOUBLE", "CharDPS", (Data) => { return Data.CharDPS.ToString(); }, (Data) => { return Data.CharDPS.ToString(usCulture); }, (Left, Right) => { return Left.CharDPS.CompareTo(Right.CharDPS); }));
            AttackType.ColumnDefs.Add("DPS", new AttackType.ColumnDef("DPS", false, "DOUBLE", "DPS", (Data) => { return Data.DPS.ToString(); }, (Data) => { return Data.DPS.ToString(usCulture); }, (Left, Right) => { return Left.DPS.CompareTo(Right.DPS); }));
            AttackType.ColumnDefs.Add("Average", new AttackType.ColumnDef("Average", true, "DOUBLE", "Average", (Data) => { return Data.Average.ToString(); }, (Data) => { return Data.Average.ToString(); }, (Left, Right) => { return Left.Average.CompareTo(Right.Average); }));
            AttackType.ColumnDefs.Add("Median", new AttackType.ColumnDef("Median", true, "BIGINT", "Median", (Data) => { return Data.Median.ToString(); }, (Data) => { return Data.Median.ToString(); }, (Left, Right) => { return Left.Median.CompareTo(Right.Median); }));
            AttackType.ColumnDefs.Add("StdDev", new AttackType.ColumnDef("StdDev", true, "DOUBLE", "StdDev", (Data) => { return Math.Sqrt(AttackTypeGetVariance(Data)).ToString(); }, (Data) => { return Math.Sqrt(AttackTypeGetVariance(Data)).ToString(); }, (Left, Right) => { return Math.Sqrt(AttackTypeGetVariance(Left)).CompareTo(Math.Sqrt(AttackTypeGetVariance(Right))); }));
            AttackType.ColumnDefs.Add("CritTypes", new AttackType.ColumnDef("CritTypes", true, "VARCHAR(32)", "CritTypes", AttackTypeGetCritTypes, AttackTypeGetCritTypes, (Left, Right) => { return AttackTypeGetCritTypes(Left).CompareTo(AttackTypeGetCritTypes(Right)); }));
            AttackType.ColumnDefs.Add("Max", new AttackType.ColumnDef("Max", true, "BIGINT", "Max", (Data) => { return Data.MaxHit.ToString(); }, (Data) => { return Data.MaxHit.ToString(); }, (Left, Right) => { return Left.MaxHit.CompareTo(Right.MaxHit); }));
            AttackType.ColumnDefs.Add("Min", new AttackType.ColumnDef("Min", true, "BIGINT", "Min", (Data) => { return Data.MinHit.ToString(); }, (Data) => { return Data.MinHit.ToString(); }, (Left, Right) => { return Left.MinHit.CompareTo(Right.MinHit); }));

            MasterSwing.ColumnDefs.Clear();
            MasterSwing.ColumnDefs.Add("EncId", new MasterSwing.ColumnDef("EncId", false, "CHAR(8)", "EncId", (Data) => { return string.Empty; }, (Data) => { return Data.ParentEncounter.EncId; }, (Left, Right) => { return 0; }));
            MasterSwing.ColumnDefs.Add("Time", new MasterSwing.ColumnDef("Time", true, "TIMESTAMP", "STime", (Data) => { return Data.Time.ToString("T"); }, (Data) => { return Data.Time.ToString("u").TrimEnd(new char[] { 'Z' }); }, (Left, Right) => { return Left.Time.CompareTo(Right.Time); }));
            MasterSwing.ColumnDefs.Add("RelativeTime", new MasterSwing.ColumnDef("RelativeTime", true, "FLOAT", "RelativeTime", (Data) => { return Data.ParentEncounter != null ? (Data.Time - Data.ParentEncounter.StartTime).ToString("g") : String.Empty; }, (Data) => { return Data.ParentEncounter != null ? (Data.Time - Data.ParentEncounter.StartTime).TotalSeconds.ToString(usCulture) : String.Empty; }, (Left, Right) => { return Left.Time.CompareTo(Right.Time); }));
            MasterSwing.ColumnDefs.Add("Attacker", new MasterSwing.ColumnDef("Attacker", true, "VARCHAR(64)", "Attacker", (Data) => { return Data.Attacker; }, (Data) => { return Data.Attacker; }, (Left, Right) => { return Left.Attacker.CompareTo(Right.Attacker); }));
            MasterSwing.ColumnDefs.Add("SwingType", new MasterSwing.ColumnDef("SwingType", false, "TINYINT", "SwingType", (Data) => { return Data.SwingType.ToString(); }, (Data) => { return Data.SwingType.ToString(); }, (Left, Right) => { return Left.SwingType.CompareTo(Right.SwingType); }));
            MasterSwing.ColumnDefs.Add("AttackType", new MasterSwing.ColumnDef("AttackType", true, "VARCHAR(64)", "AttackType", (Data) => { return Data.AttackType; }, (Data) => { return Data.AttackType; }, (Left, Right) => { return Left.AttackType.CompareTo(Right.AttackType); }));
            MasterSwing.ColumnDefs.Add("DamageType", new MasterSwing.ColumnDef("DamageType", true, "VARCHAR(64)", "DamageType", (Data) => { return Data.DamageType; }, (Data) => { return Data.DamageType; }, (Left, Right) => { return Left.DamageType.CompareTo(Right.DamageType); }));
            MasterSwing.ColumnDefs.Add("Victim", new MasterSwing.ColumnDef("Victim", true, "VARCHAR(64)", "Victim", (Data) => { return Data.Victim; }, (Data) => { return Data.Victim; }, (Left, Right) => { return Left.Victim.CompareTo(Right.Victim); }));
            MasterSwing.ColumnDefs.Add("DamageNum", new MasterSwing.ColumnDef("DamageNum", false, "BIGINT", "Damage", (Data) => {
                switch((long)Data.Damage)
                {
                    case -1:
                        return String.Empty;
                    default:
                        return ((long)Data.Damage).ToString();
                }
            }
            , 
            (Data) => {
                switch ((long)Data.Damage)
                {
                    case -1:
                        return String.Empty;
                    default:
                        return ((long)Data.Damage).ToString();
                }
            }, 
            (Left, Right) => { 
                return Left.Damage.CompareTo(Right.Damage); 
            }
            )
                );
            MasterSwing.ColumnDefs.Add("Damage", new MasterSwing.ColumnDef("Damage", true, "VARCHAR(128)", "DamageString", (Data) => { return Data.Damage.ToString(); }, (Data) => { return Data.Damage.ToString(); }, (Left, Right) => { return Left.Damage.CompareTo(Right.Damage); }));
            MasterSwing.ColumnDefs.Add("Critical", new MasterSwing.ColumnDef("Critical", false, "BOOLEAN", "Critical", (Data) => { return Data.Critical.ToString(); }, (Data) => { return Data.Critical.ToString(usCulture)[0].ToString(); }, (Left, Right) => { return Left.Critical.CompareTo(Right.Critical); }));
            MasterSwing.ColumnDefs.Add("Special", new MasterSwing.ColumnDef("Special", true, "VARCHAR(90)", "Special", (Data) => { return Data.Special; }, (Data) => { return Data.Special; }, (Left, Right) => { return Left.Special.CompareTo(Left.Special); }));
            MasterSwing.ColumnDefs.Add("Overheal", new MasterSwing.ColumnDef("Overheal", true, "BIGINT", "Overheal", (Data) => { return Data.Tags.ContainsKey("overheal") ? ((long)Data.Tags["overheal"]).ToString() : string.Empty; }, (Data) => { return Data.Tags.ContainsKey("overheal") ? ((long)Data.Tags["overheal"]).ToString() : string.Empty; }, (Left, Right) =>
            {
                return (Left.Tags.ContainsKey("overheal") && Right.Tags.ContainsKey("overheal")) ? ((long)Left.Tags["overheal"]).CompareTo((long)Right.Tags["overheal"]) : 0;
            }));

            foreach (KeyValuePair<string, MasterSwing.ColumnDef> pair in MasterSwing.ColumnDefs)
                pair.Value.GetCellForeColor = (Data) => { return GetSwingTypeColor((EverQuestSwingType)Data.SwingType); };

            ActGlobals.oFormActMain.ValidateLists();
            ActGlobals.oFormActMain.ValidateTableSetup();
        }

        #region Statistic processing
        //Statistics specific processing
        //Backstep function for time series processing
        private double[] BackStep(AttackType Data, int backstep)
        {
            if (Data.Items.Count > backstep)
            {
                double[] values = new double[Data.Items.Count - backstep];
                for (int i = 0; i < Data.Items.Count; i++)
                    values[i] = Data.Items[i + backstep].Damage.Number - Data.Items[i].Damage.Number;
                return values;
            }
            else
                return new double[] { default };
        }
        //Variance calculation for attack damage
        private double AttackTypeGetVariance(AttackType Data)
        {
            List<MasterSwing> ms = Data.Items.ToList().Where((item) => item.Damage.Number >= 0).ToList();
            double average;
            lock (varianceChkBxLockObject)
            {
                if (!populationVariance && Data.Items.Count > 1)
                {
                    average = ms.Select((item) => item.Damage.Number).Average();
                    return ms.Sum((item) =>
                        {
                            return Math.Pow(average - item.Damage.Number, 2.0);
                        }) / (ms.Count - 1);
                }
                else if (populationVariance && Data.Items.Count > 0)
                {
                    average = ms.Select((item) => item.Damage.Number).Average();
                    return ms.Sum((item) =>
                    {
                        return Math.Pow(average - item.Damage.Number, 2.0);
                    }) / ms.Count;
                }
                else
                    return default;
            }
        }
        #endregion

        //modified to display an empty string in the event no special type of attack is detected by the regex processing
        private string CombatantDataGetCritTypes(CombatantData Data)
        {
            if (Data.AllOut.TryGetValue(ActGlobals.ActLocalization.LocalizationStrings["attackTypeTerm-all"].DisplayedText, out AttackType at))
            {
                return AttackTypeGetCritTypes(at);
            }
            else
                return String.Empty;
        }

        private string DamageTypeDataGetCritTypes(DamageTypeData Data)
        {
            if (Data.Items.TryGetValue(ActGlobals.ActLocalization.LocalizationStrings["attackTypeTerm-all"].DisplayedText, out AttackType at))
            {
                return AttackTypeGetCritTypes(at);
            }
            else
                return String.Empty;
        }

        private Color GetSwingTypeColor(EverQuestSwingType eqst)
        {
            switch (eqst)
            {
                case EverQuestSwingType.Melee:
                    return Color.DarkViolet;
                case EverQuestSwingType.NonMelee:
                    return Color.DarkSlateGray;
                case EverQuestSwingType.InstantHealing:
                    return Color.DodgerBlue;
                case EverQuestSwingType.HealOverTime:
                    return Color.GreenYellow;
                case EverQuestSwingType.Bane:
                    return Color.Honeydew;
                case EverQuestSwingType.WardInstantHealing:
                    return Color.LemonChiffon;
                case EverQuestSwingType.WardHealOverTime:
                    return Color.LightSeaGreen;
                case EverQuestSwingType.DamageOverTimeSpell:
                    return Color.Olive;
                case EverQuestSwingType.DirectDamageSpell:
                    return Color.Yellow;
                case EverQuestSwingType.PetMelee:
                    return Color.Violet;
                case EverQuestSwingType.PetNonMelee:
                    return Color.CornflowerBlue;
                case EverQuestSwingType.WarderMelee:
                    return Color.MistyRose;
                case EverQuestSwingType.WarderNonMelee:
                    return Color.GreenYellow;
                case EverQuestSwingType.WarderDirectDamageSpell:
                    return Color.ForestGreen;
                case EverQuestSwingType.WarderDamageOverTimeSpell:
                    return Color.Thistle;
                default:
                    return Color.Black;
            }
        }

        private string AttackTypeGetCritTypes(AttackType Data)
        {
            List<MasterSwing> ms = Data.Items.ToList().Where((item) => item.Damage >= 0).ToList();
            int CripplingBlowCount = 0;
            int LockedCount = 0;
            int CriticalCount = 0;
            int StrikethroughCount = 0;
            int RiposteCount = 0;
            int NonDefinedCount = 0;
            int FlurryCount = 0;
            int LuckyCount = 0;
            int DoubleBowShotCount = 0;
            int TwincastCount = 0;
            int WildRampageCount = 0;
            int FinishingBlowCount = 0;
            int count = ms.Count;

            FinishingBlowCount = ms.Where((finishingBlow) =>
            {
                return finishingBlow.Special.Contains(EverQuestDPSPluginResource.FinishingBlow);
            }).Count();
            CriticalCount = ms.Where((critital) =>
            {
                return critital.Critical;
            }).Count();
            FlurryCount = ms.Where((flurry) =>
            {
                return flurry.Special.Contains(EverQuestDPSPluginResource.Flurry);
            }).Count();
            LuckyCount = ms.Where((lucky) =>
            {
                return lucky.Special.Contains(EverQuestDPSPluginResource.Lucky);
            }).Count();
            CripplingBlowCount = ms.Where((cripplingBlow) =>
            {
                return cripplingBlow.Special.Contains(EverQuestDPSPluginResource.CripplingBlow);
            }).Count();
            LockedCount = ms.Where((locked) =>
            {
                return locked.Special.Contains(EverQuestDPSPluginResource.Locked);
            }).Count();
            StrikethroughCount = ms.Where((srikethrough) =>
            {
                return srikethrough.Special.Contains(EverQuestDPSPluginResource.Strikethrough);
            }).Count();
            RiposteCount = ms.Where((riposte) =>
            {
                return riposte.Special.Contains(EverQuestDPSPluginResource.Riposte);
            }).Count();
            DoubleBowShotCount = ms.Where((doubleBowShot) =>
            {
                return doubleBowShot.Special.Contains(EverQuestDPSPluginResource.DoubleBowShot);
            }).Count();
            TwincastCount = ms.Where((twincast) =>
            {
                return twincast.Special.Contains(EverQuestDPSPluginResource.Twincast);
            }).Count();
            WildRampageCount = ms.Where((twincast) =>
            {
                return twincast.Special.Contains(EverQuestDPSPluginResource.WildRampage);
            }).Count();
            NonDefinedCount = ms.Where((nondefined) =>
            {
                return !nondefined.Special.Contains(EverQuestDPSPluginResource.Twincast) &&
                    !nondefined.Special.Contains(EverQuestDPSPluginResource.DoubleBowShot) &&
                    !nondefined.Special.Contains(EverQuestDPSPluginResource.Riposte) &&
                    !nondefined.Special.Contains(EverQuestDPSPluginResource.CripplingBlow) &&
                    !nondefined.Special.Contains(EverQuestDPSPluginResource.Lucky) &&
                    !nondefined.Special.Contains(EverQuestDPSPluginResource.Flurry) &&
                    !nondefined.Special.Contains(EverQuestDPSPluginResource.Critical) &&
                    !nondefined.Special.Contains(EverQuestDPSPluginResource.WildRampage) &&
                    !nondefined.Special.Contains(EverQuestDPSPluginResource.CripplingBlow) &&
                    !nondefined.Special.Contains(EverQuestDPSPluginResource.Strikethrough) &&
                    !nondefined.Special.Contains(EverQuestDPSPluginResource.FinishingBlow)
                    && nondefined.Special.Length > ActGlobals.ActLocalization.LocalizationStrings["specialAttackTerm-none"].DisplayedText.Length;

            }).Count();

            float CripplingBlowPerc = ((float)CripplingBlowCount / (float)count) * 100f;
            float LockedPerc = ((float)LockedCount / (float)count) * 100f;
            float CriticalPerc = ((float)CriticalCount / (float)count) * 100f;
            float NonDefinedPerc = ((float)NonDefinedCount / (float)count) * 100f;
            float StrikethroughPerc = ((float)StrikethroughCount / (float)count) * 100f;
            float RipostePerc = ((float)RiposteCount / (float)count) * 100f;
            float FlurryPerc = ((float)FlurryCount / (float)count) * 100f;
            float LuckyPerc = ((float)LuckyCount / (float)count) * 100f;
            float DoubleBowShotPerc = ((float)DoubleBowShotCount / (float)count) * 100f;
            float TwincastPerc = ((float)TwincastCount / (float)count) * 100f;
            float WildRampagePerc = ((float)WildRampageCount / (float)count) * 100f;
            float FinishingBlowPerc = ((float)FinishingBlowCount / (float)count) * 100f;

            return $"{CripplingBlowPerc:000.0}%CB-{LockedPerc:000.0}%Locked-{CriticalPerc:000.0}%C-{StrikethroughPerc:000.0}%S-{RipostePerc:000.0}%R-{FlurryPerc:000.0}%F-{LuckyPerc:000.0}%Lucky-{DoubleBowShotPerc:000.0}%DB-{TwincastPerc:000.0}%TC-{WildRampagePerc:000.0}%WR-{FinishingBlowPerc:000.0}%FB-{NonDefinedPerc:000.0}%ND";
        }

        private string GetAttackTypeSwingType(AttackType Data)
        {
            int? swingType = null;
            List<String> swingTypes = Data.Items.ToList().Select(o => o.AttackType).Distinct().ToList();
            List<MasterSwing> cachedItems = new List<MasterSwing>();
            for (int i = 0; i < Data.Items.Count; i++)
            {
                MasterSwing s = Data.Items[i];
                if (swingTypes.Contains(Data.Items[i].SwingType.ToString()) == false)
                    swingTypes.Add(Data.Items[i].SwingType.ToString());
            }
            if (swingTypes.Count == 1)
                swingType = Data.Items[0].SwingType;
            if (swingType == null)
                return String.Empty;
            else
                return swingType == null ? String.Empty : swingType.ToString();
        }

        #region User Interface Update code
        void changeLblStatus(String status)
        {
            switch (lblStatus.InvokeRequired)
            {
                case true:
                    this.lblStatus.Invoke(new Action(() =>
                    {
                        this.lblStatus.Text = status;
                    }));
                    break;
                case false:
                    this.lblStatus.Text = status;
                    break;
            }
        }

        public void ChangeNonmatchFormCheckBox(bool Checked)
        {
            if (nonMatchVisibleChkbx.InvokeRequired)
            {
                nonMatchVisibleChkbx.Invoke(new Action(() =>
                {
                    nonMatchVisibleChkbx.Checked = Checked;
                }));
            }
            else
            {
                nonMatchVisibleChkbx.Checked = Checked;
            }
        }

        void AddLogLineToNonMatch(String message)
        {
            if (nm.InvokeRequired)
            {
                nm.Invoke(new Action(() =>
                {
                    nm.addLogLineToForm(message);
                }));
            }
            else
                nm.addLogLineToForm(message);
        }
        //checkbox processing event for population or sample variance
        private void VarianceChkBx_CheckedChanged(object sender, EventArgs e)
        {
            lock (varianceChkBxLockObject)
                this.populationVariance = (sender as CheckBox).Checked;
            switch (this.populationVariance)
            {
                case true:
                    if (lblStatus.InvokeRequired)
                        this.lblStatus.Invoke(new Action(() => { this.lblStatus.Text = $"Reporting population variance {EverQuestDPSPluginResource.pluginName}"; }));
                    else
                        this.lblStatus.Text = $"Reporting population variance {EverQuestDPSPluginResource.pluginName}";
                    break;
                case false:
                    if (lblStatus.InvokeRequired)
                        this.lblStatus.Invoke(new Action(() => { this.lblStatus.Text = $"Reporting sample variance {EverQuestDPSPluginResource.pluginName}"; }));
                    else
                        this.lblStatus.Text = $"Reporting sample variance {EverQuestDPSPluginResource.pluginName}";
                    break;
            }
        }

        private void nonMatchVisible_CheckedChanged(object sender, EventArgs e)
        {
            this.nonMatchVisible = (sender as CheckBox).Checked;
            if (nm == null || nm.IsDisposed) { nm = new nonmatch(this); }
            switch (this.nonMatchVisible)
            {
                case true:
                    (sender as CheckBox).Enabled = false;
                    if (this.nm.InvokeRequired)
                        nm.Invoke(new Action(() =>
                        {
                            nm.Visible = this.nonMatchVisible;
                        }));
                    else
                        nm.Visible = this.nonMatchVisible;
                    (sender as CheckBox).Enabled = true;
                    break;
                case false:
                    (sender as CheckBox).Enabled = false;
                    if (this.nm.InvokeRequired)
                        nm.Invoke(new Action(() =>
                        {
                            nm.Visible = this.nonMatchVisible;
                        }));
                    else
                        nm.Visible = this.nonMatchVisible;
                    (sender as CheckBox).Enabled = true;
                    break;
                default:
                    break;
            }
        }
        #endregion

        private string EncounterFormatSwitch(EncounterData Data, List<CombatantData> SelectiveAllies, string VarName, string Extra)
        {
            long damage = 0;
            long healed = 0;
            int swings = 0;
            int hits = 0;
            int crits = 0;
            int heals = 0;
            int critheals = 0;
            int cures = 0;
            int misses = 0;
            int hitfail = 0;
            float tohit = 0;
            double dps;
            double hps;
            long healstaken = 0;
            long damagetaken = 0;
            //long powerdrain = 0;
            //long powerheal = 0;
            int kills = 0;
            int deaths = 0;

            switch (VarName)
            {
                case "maxheal":
                    return Data.GetMaxHeal(true, false, false);
                case "MAXHEAL":
                    return Data.GetMaxHeal(false, false, false);
                case "maxheal-*":
                    return Data.GetMaxHeal(true, false, true);
                case "MAXHEAL-*":
                    return Data.GetMaxHeal(false, false, true);
                case "maxhealward":
                    return Data.GetMaxHeal(true, true, false);
                case "MAXHEALWARD":
                    return Data.GetMaxHeal(false, true, false);
                case "maxhealward-*":
                    return Data.GetMaxHeal(true, true, true);
                case "MAXHEALWARD-*":
                    return Data.GetMaxHeal(false, true, true);
                case "maxhit":
                    return Data.GetMaxHit(true, false);
                case "MAXHIT":
                    return Data.GetMaxHit(false, false);
                case "maxhit-*":
                    return Data.GetMaxHit(true, true);
                case "MAXHIT-*":
                    return Data.GetMaxHit(false, true);
                case "duration":
                    if (ActGlobals.wallClockDuration)
                    {
                        if (Data.Active)
                        {
                            if (ActGlobals.oFormActMain.LastEstimatedTime > Data.StartTime)
                            {
                                TimeSpan wallDuration = ActGlobals.oFormActMain.LastEstimatedTime - Data.StartTime;
                                if (wallDuration.Hours == 0)
                                    return String.Format("{0:00}:{1:00}", wallDuration.Minutes, wallDuration.Seconds);
                                else
                                    return String.Format("{0:00}:{1:00}:{2:00}", wallDuration.Hours, wallDuration.Minutes, wallDuration.Seconds);
                            }
                            else
                            {
                                return "00:00";
                            }
                        }
                        else
                        {
                            return Data.DurationS;
                        }
                    }
                    else
                        return Data.DurationS;
                case "DURATION":
                    if (ActGlobals.wallClockDuration)
                    {
                        if (Data.Active)
                        {
                            if (ActGlobals.oFormActMain.LastEstimatedTime > Data.StartTime)
                            {
                                TimeSpan wallDuration = ActGlobals.oFormActMain.LastEstimatedTime - Data.StartTime;
                                return ((int)wallDuration.TotalSeconds).ToString("0");
                            }
                            else
                            {
                                return "0";
                            }
                        }
                        else
                        {
                            return ((int)Data.Duration.TotalSeconds).ToString("0");
                        }
                    }
                    else
                        return Data.Duration.TotalSeconds.ToString("0");
                case "damage":
                    foreach (CombatantData cd in SelectiveAllies)
                        damage += cd.Damage;
                    return damage.ToString();
                case "damage-m":
                    foreach (CombatantData cd in SelectiveAllies)
                        damage += cd.Damage;
                    return (damage / 1000000.0).ToString("0.00");
                case "damage-b":
                    foreach (CombatantData cd in SelectiveAllies)
                        damage += cd.Damage;
                    return (damage / 1000000000.0).ToString("0.00");
                case "damage-*":
                    foreach (CombatantData cd in SelectiveAllies)
                        damage += cd.Damage;
                    return ActGlobals.oFormActMain.CreateDamageString(damage, true, true);
                case "DAMAGE-k":
                    foreach (CombatantData cd in SelectiveAllies)
                        damage += cd.Damage;
                    return (damage / 1000.0).ToString("0");
                case "DAMAGE-m":
                    foreach (CombatantData cd in SelectiveAllies)
                        damage += cd.Damage;
                    return (damage / 1000000.0).ToString("0");
                case "DAMAGE-b":
                    foreach (CombatantData cd in SelectiveAllies)
                        damage += cd.Damage;
                    return (damage / 1000000000.0).ToString("0");
                case "DAMAGE-*":
                    foreach (CombatantData cd in SelectiveAllies)
                        damage += cd.Damage;
                    return ActGlobals.oFormActMain.CreateDamageString(damage, true, false);
                case "healed":
                    foreach (CombatantData cd in SelectiveAllies)
                        healed += cd.Healed;
                    return healed.ToString();
                case "healed-*":
                    foreach (CombatantData cd in SelectiveAllies)
                        healed += cd.Healed;
                    return ActGlobals.oFormActMain.CreateDamageString(healed, true, true);
                case "swings":
                    foreach (CombatantData cd in SelectiveAllies)
                        swings += cd.Swings;
                    return swings.ToString();
                case "hits":
                    foreach (CombatantData cd in SelectiveAllies)
                        hits += cd.Hits;
                    return hits.ToString();
                case "crithits":
                    foreach (CombatantData cd in SelectiveAllies)
                        crits += cd.CritHits;
                    return crits.ToString();
                case "crithit%":
                    foreach (CombatantData cd in SelectiveAllies)
                        crits += cd.CritHits;
                    foreach (CombatantData cd in SelectiveAllies)
                        hits += cd.Hits;
                    float crithitperc = (float)crits / (float)hits;
                    return crithitperc.ToString("0'%");
                case "heals":
                    foreach (CombatantData cd in SelectiveAllies)
                        heals += cd.Heals;
                    return heals.ToString();
                case "critheals":
                    foreach (CombatantData cd in SelectiveAllies)
                        critheals += cd.CritHits;
                    return critheals.ToString();
                case "critheal%":
                    foreach (CombatantData cd in SelectiveAllies)
                        critheals += cd.CritHeals;
                    foreach (CombatantData cd in SelectiveAllies)
                        heals += cd.Heals;
                    float crithealperc = (float)critheals / (float)heals;
                    return crithealperc.ToString("0'%");
                case "cures":
                    foreach (CombatantData cd in SelectiveAllies)
                        cures += cd.CureDispels;
                    return cures.ToString();
                case "misses":
                    foreach (CombatantData cd in SelectiveAllies)
                        misses += cd.Misses;
                    return misses.ToString();
                case "hitfailed":
                    foreach (CombatantData cd in SelectiveAllies)
                        hitfail += cd.Blocked;
                    return hitfail.ToString();
                case "TOHIT":
                    foreach (CombatantData cd in SelectiveAllies)
                        tohit += cd.ToHit;
                    tohit /= SelectiveAllies.Count;
                    return tohit.ToString("0");
                case "DPS":
                case "ENCDPS":
                    foreach (CombatantData cd in SelectiveAllies)
                        damage += cd.Damage;
                    dps = damage / Data.Duration.TotalSeconds;
                    return dps.ToString("0");
                case "DPS-*":
                case "ENCDPS-*":
                    foreach (CombatantData cd in SelectiveAllies)
                        damage += cd.Damage;
                    dps = damage / Data.Duration.TotalSeconds;
                    return ActGlobals.oFormActMain.CreateDamageString((long)dps, true, false);
                case "DPS-k":
                case "ENCDPS-k":
                    foreach (CombatantData cd in SelectiveAllies)
                        damage += cd.Damage;
                    dps = damage / Data.Duration.TotalSeconds;
                    return (dps / 1000.0).ToString("0");
                case "ENCDPS-m":
                    foreach (CombatantData cd in SelectiveAllies)
                        damage += cd.Damage;
                    dps = damage / Data.Duration.TotalSeconds;
                    return (dps / 1000000.0).ToString("0");
                case "ENCHPS":
                    foreach (CombatantData cd in SelectiveAllies)
                        healed += cd.Healed;
                    hps = healed / Data.Duration.TotalSeconds;
                    return hps.ToString("0");
                case "ENCHPS-k":
                    foreach (CombatantData cd in SelectiveAllies)
                        healed += cd.Healed;
                    hps = healed / Data.Duration.TotalSeconds;
                    return (hps / 1000.0).ToString("0");
                case "ENCHPS-m":
                    foreach (CombatantData cd in SelectiveAllies)
                        healed += cd.Healed;
                    hps = healed / Data.Duration.TotalSeconds;
                    return (hps / 1000000.0).ToString("0");
                case "ENCHPS-*":
                    foreach (CombatantData cd in SelectiveAllies)
                        healed += cd.Healed;
                    hps = healed / Data.Duration.TotalSeconds;
                    return ActGlobals.oFormActMain.CreateDamageString((long)hps, true, false);
                case "tohit":
                    foreach (CombatantData cd in SelectiveAllies)
                        tohit += cd.ToHit;
                    tohit /= SelectiveAllies.Count;
                    return tohit.ToString("F");
                case "dps":
                case "encdps":
                    foreach (CombatantData cd in SelectiveAllies)
                        damage += cd.Damage;
                    dps = damage / Data.Duration.TotalSeconds;
                    return dps.ToString("F");
                case "dps-k":
                case "encdps-k":
                    foreach (CombatantData cd in SelectiveAllies)
                        damage += cd.Damage;
                    dps = damage / Data.Duration.TotalSeconds;
                    return (dps / 1000.0).ToString("F");
                case "encdps-m":
                    foreach (CombatantData cd in SelectiveAllies)
                        damage += cd.Damage;
                    dps = damage / Data.Duration.TotalSeconds;
                    return (dps / 1000000.0).ToString("F");
                case "encdps-*":
                    foreach (CombatantData cd in SelectiveAllies)
                        damage += cd.Damage;
                    dps = damage / Data.Duration.TotalSeconds;
                    return ActGlobals.oFormActMain.CreateDamageString((long)dps, true, true);
                case "enchps":
                    foreach (CombatantData cd in SelectiveAllies)
                        healed += cd.Healed;
                    hps = healed / Data.Duration.TotalSeconds;
                    return hps.ToString("F");
                case "enchps-k":
                    foreach (CombatantData cd in SelectiveAllies)
                        healed += cd.Healed;
                    hps = healed / Data.Duration.TotalSeconds;
                    return (hps / 1000.0).ToString("F");
                case "enchps-m":
                    foreach (CombatantData cd in SelectiveAllies)
                        healed += cd.Healed;
                    hps = healed / Data.Duration.TotalSeconds;
                    return (hps / 1000000.0).ToString("F");
                case "enchps-*":
                    foreach (CombatantData cd in SelectiveAllies)
                        healed += cd.Healed;
                    hps = healed / Data.Duration.TotalSeconds;
                    return ActGlobals.oFormActMain.CreateDamageString((long)hps, true, true);
                case "healstaken":
                    foreach (CombatantData cd in SelectiveAllies)
                        healstaken += cd.HealsTaken;
                    return healstaken.ToString();
                case "healstaken-*":
                    foreach (CombatantData cd in SelectiveAllies)
                        healstaken += cd.HealsTaken;
                    return ActGlobals.oFormActMain.CreateDamageString(healstaken, true, true);
                case "damagetaken":
                    foreach (CombatantData cd in SelectiveAllies)
                        damagetaken += cd.DamageTaken;
                    return damagetaken.ToString();
                case "damagetaken-*":
                    foreach (CombatantData cd in SelectiveAllies)
                        damagetaken += cd.DamageTaken;
                    return ActGlobals.oFormActMain.CreateDamageString(damagetaken, true, true);
                case "kills":
                    foreach (CombatantData cd in SelectiveAllies)
                        kills += cd.Kills;
                    return kills.ToString();
                case "deaths":
                    foreach (CombatantData cd in SelectiveAllies)
                        deaths += cd.Deaths;
                    return deaths.ToString();
                case "title":
                    return Data.Title;

                default:
                    return VarName;
            }
        }

    }
}
