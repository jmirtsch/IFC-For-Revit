//
// BIM IFC export alternate UI library: this library works with Autodesk(R) Revit(R) to provide an alternate user interface for the export of IFC files from Revit.
// Copyright (C) 2015  Autodesk, Inc.
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.UI;

namespace BIM.IFC.Export.UI
{
    /// <summary>
    /// The IFC export UI options window.
    /// </summary>
    public partial class IFCExporterUIWindow : Window
    {
        /// <summary>
        /// The map contains the configurations.
        /// </summary>
        IFCExportConfigurationsMap m_configurationsMap;

        /// <summary>
        /// The file to store the previous window bounds.
        /// </summary>
        string m_SettingFile = "IFCExporterUIWindowSettings_v17.txt";    // update the file when resize window bounds.

        /// <summary>
        /// Constructs a new IFC export options window.
        /// </summary>
        /// <param name="exportOptions">The export options that will be populated by settings in the window.</param>
        /// <param name="currentViewId">The Revit current view id.</param>
        public IFCExporterUIWindow(IFCExportConfigurationsMap configurationsMap, String currentConfigName)
        {
            InitializeComponent();

            RestorePreviousWindow();

            m_configurationsMap = configurationsMap;

            InitializeConfigurationList(currentConfigName);

            IFCExportConfiguration originalConfiguration = m_configurationsMap[currentConfigName];
            InitializeConfigurationOptions();
            UpdateActiveConfigurationOptions(originalConfiguration);
        }

        /// <summary>
        /// Restores the previous window. If no previous window found, place on the left top.
        /// </summary>
        private void RestorePreviousWindow()
        {
            // Refresh restore bounds from previous window opening
            Rect restoreBounds = IFCUISettings.LoadWindowBounds(m_SettingFile);
            if (restoreBounds != new Rect())
            {
                this.Left = restoreBounds.Left;
                this.Top = restoreBounds.Top;
                this.Width = restoreBounds.Width;
                this.Height = restoreBounds.Height;
            }
        }

        /// <summary>
        /// Initializes the listbox by filling the available configurations from the map.
        /// </summary>
        /// <param name="currentConfigName">The current configuration name.</param>
        private void InitializeConfigurationList(String currentConfigName)
        {
            foreach (IFCExportConfiguration configuration in m_configurationsMap.Values)
            {
                listBoxConfigurations.Items.Add(configuration);
                if (configuration.Name == currentConfigName)
                    listBoxConfigurations.SelectedItem = configuration;
            }
        }

        /// <summary>
        /// Updates and resets the listbox.
        /// </summary>
        /// <param name="currentConfigName">The current configuration name.</param>
        private void UpdateConfigurationsList(String currentConfigName)
        {
            listBoxConfigurations.Items.Clear();
            InitializeConfigurationList(currentConfigName);
        }

        /// <summary>
        /// Initializes the comboboxes via the configuration options.
        /// </summary>
        private void InitializeConfigurationOptions()
        {
            comboboxIfcType.Items.Add(new IFCVersionAttributes(IFCVersion.IFC2x2));
            comboboxIfcType.Items.Add(new IFCVersionAttributes(IFCVersion.IFC2x3));
            comboboxIfcType.Items.Add(new IFCVersionAttributes(IFCVersion.IFC2x3CV2));
            comboboxIfcType.Items.Add(new IFCVersionAttributes(IFCVersion.IFCCOBIE));
            comboboxIfcType.Items.Add(new IFCVersionAttributes(IFCVersion.IFCBCA));
            comboboxIfcType.Items.Add(new IFCVersionAttributes(IFCVersion.IFC4));
            
            foreach (IFCFileFormat fileType in Enum.GetValues(typeof(IFCFileFormat)))
            {
                IFCFileFormatAttributes item = new IFCFileFormatAttributes(fileType);
                comboboxFileType.Items.Add(item);
            }

            for (int level = 0; level <= 2; level++)
            {
                IFCSpaceBoundariesAttributes item = new IFCSpaceBoundariesAttributes(level);
                comboboxSpaceBoundaries.Items.Add(item);
            }

            PhaseArray phaseArray = IFCCommandOverrideApplication.TheDocument.Phases;
            comboboxActivePhase.Items.Add(new IFCPhaseAttributes(ElementId.InvalidElementId));  // Default.
            foreach (Phase phase in phaseArray)
            {
                comboboxActivePhase.Items.Add(new IFCPhaseAttributes(phase.Id));
            }

            // Initialize level of detail combo box
            comboBoxLOD.Items.Add(Properties.Resources.DetailLevelExtraLow);
            comboBoxLOD.Items.Add(Properties.Resources.DetailLevelLow);
            comboBoxLOD.Items.Add(Properties.Resources.DetailLevelMedium);
            comboBoxLOD.Items.Add(Properties.Resources.DetailLevelHigh);
        }

        private void UpdatePhaseAttributes(IFCExportConfiguration configuration)
        {
            if (configuration.VisibleElementsOfCurrentView)
            {
                UIDocument uiDoc = new UIDocument(IFCCommandOverrideApplication.TheDocument);
                Parameter currPhase = uiDoc.ActiveView.get_Parameter(BuiltInParameter.VIEW_PHASE);
                if (currPhase != null)
                    configuration.ActivePhaseId = currPhase.AsElementId();
                else
                    configuration.ActivePhaseId = ElementId.InvalidElementId;
            }

            if (!IFCPhaseAttributes.Validate(configuration.ActivePhaseId))
                configuration.ActivePhaseId = ElementId.InvalidElementId;
            
            foreach (IFCPhaseAttributes attribute in comboboxActivePhase.Items.Cast<IFCPhaseAttributes>())
            {
                if (configuration.ActivePhaseId == attribute.PhaseId)
                {
                    comboboxActivePhase.SelectedItem = attribute;
                    break;
                }
            }

            comboboxActivePhase.IsEnabled = !configuration.VisibleElementsOfCurrentView;
        }

        /// <summary>
        /// Updates the active configuration options to the controls.
        /// </summary>
        /// <param name="configuration">The active configuration.</param>
        private void UpdateActiveConfigurationOptions(IFCExportConfiguration configuration)
        {
            foreach (IFCVersionAttributes attribute in comboboxIfcType.Items.Cast<IFCVersionAttributes>())
            {
                if (attribute.Version == configuration.IFCVersion)
                {
                    comboboxIfcType.SelectedItem = attribute;
                    break;
                }
            }

            foreach (IFCFileFormatAttributes format in comboboxFileType.Items.Cast<IFCFileFormatAttributes>())
            {
                if (configuration.IFCFileType == format.FileType)
                {
                    comboboxFileType.SelectedItem = format;
                    break;
                }
            }

            foreach (IFCSpaceBoundariesAttributes attribute in comboboxSpaceBoundaries.Items.Cast<IFCSpaceBoundariesAttributes>())
            {
                if (configuration.SpaceBoundaries == attribute.Level)
                {
                    comboboxSpaceBoundaries.SelectedItem = attribute;
                    break;
                }
            }

            UpdatePhaseAttributes(configuration);

            checkboxExportBaseQuantities.IsChecked = configuration.ExportBaseQuantities;
            checkboxSplitWalls.IsChecked = configuration.SplitWallsAndColumns;
            checkbox2dElements.IsChecked = configuration.Export2DElements;
            checkboxInternalPropertySets.IsChecked = configuration.ExportInternalRevitPropertySets;
            checkboxIFCCommonPropertySets.IsChecked = configuration.ExportIFCCommonPropertySets;
            checkboxVisibleElementsCurrView.IsChecked = configuration.VisibleElementsOfCurrentView;
            checkBoxUse2DRoomVolumes.IsChecked = configuration.Use2DRoomBoundaryForVolume;
            checkBoxFamilyAndTypeName.IsChecked = configuration.UseFamilyAndTypeNameForReference;
            checkBoxExportPartsAsBuildingElements.IsChecked = configuration.ExportPartsAsBuildingElements;
            checkBoxUseActiveViewGeometry.IsChecked = configuration.UseActiveViewGeometry;
            checkboxExportBoundingBox.IsChecked = configuration.ExportBoundingBox;
            checkboxExportSolidModelRep.IsChecked = configuration.ExportSolidModelRep;
            checkboxExportSchedulesAsPsets.IsChecked = configuration.ExportSchedulesAsPsets;
            checkboxExportUserDefinedPset.IsChecked = configuration.ExportUserDefinedPsets;
            userDefinedPropertySetFileName.Text = configuration.ExportUserDefinedPsetsFileName;
            checkBoxExportLinkedFiles.IsChecked = configuration.ExportLinkedFiles;
            checkboxIncludeIfcSiteElevation.IsChecked = configuration.IncludeSiteElevation;
            checkboxStoreIFCGUID.IsChecked = configuration.StoreIFCGUID;
            checkBoxExportRoomsInView.IsChecked = configuration.ExportRoomsInView;
            comboBoxLOD.SelectedIndex = (int)(Math.Round(configuration.TessellationLevelOfDetail * 4) - 1);
            UIElement[] configurationElements = new UIElement[]{comboboxIfcType, 
                                                                comboboxFileType, 
                                                                comboboxSpaceBoundaries, 
                                                                checkboxExportBaseQuantities,
                                                                checkboxSplitWalls,
                                                                checkbox2dElements,
                                                                checkboxInternalPropertySets,
                                                                checkboxIFCCommonPropertySets,
                                                                checkboxVisibleElementsCurrView,
                                                                checkBoxExportPartsAsBuildingElements,
                                                                checkBoxUse2DRoomVolumes,
                                                                checkBoxFamilyAndTypeName,
                                                                checkboxExportBoundingBox,
                                                                checkboxExportSolidModelRep,
                                                                checkBoxExportLinkedFiles,
                                                                checkboxIncludeIfcSiteElevation,
                                                                checkboxStoreIFCGUID,
                                                                checkboxExportSchedulesAsPsets,
                                                                checkBoxExportRoomsInView,
                                                                checkBoxLevelOfDetails,
                                                                comboboxActivePhase,
                                                                checkboxExportUserDefinedPset,
                                                                userDefinedPropertySetFileName,
                                                                buttonBrowse,
                                                                comboBoxLOD,
                                                                checkBoxUseActiveViewGeometry,
                                                                checkBoxExportSpecificSchedules
                                                                };
            foreach (UIElement element in configurationElements)
            {
                element.IsEnabled = !configuration.IsBuiltIn;
            }
            comboboxActivePhase.IsEnabled = comboboxActivePhase.IsEnabled && !configuration.VisibleElementsOfCurrentView;
            userDefinedPropertySetFileName.IsEnabled = userDefinedPropertySetFileName.IsEnabled && configuration.ExportUserDefinedPsets;
            buttonBrowse.IsEnabled = buttonBrowse.IsEnabled && configuration.ExportUserDefinedPsets;

            // ExportRoomsInView option will only be enabled if it is not currently disabled AND the "export elements visible in view" option is checked
            bool? cboVisibleElementInCurrentView = checkboxVisibleElementsCurrView.IsChecked;
            checkBoxExportRoomsInView.IsEnabled = checkBoxExportRoomsInView.IsEnabled && cboVisibleElementInCurrentView.HasValue ? cboVisibleElementInCurrentView.Value : false;

            // This is to enable the Tessellation related checkbox for Reference view only.
            if (string.Compare(configuration.Name, "IFC4 Reference View") == 0)
            {
                DocPanel_tessellation.IsEnabled = true;
                comboBoxLOD.IsEnabled = true;
            }
        }

        /// <summary>
        /// Updates the controls.
        /// </summary>
        /// <param name="isBuiltIn">Value of whether the configuration is builtIn or not.</param>
        /// <param name="isInSession">Value of whether the configuration is in-session or not.</param>
        private void UpdateConfigurationControls(bool isBuiltIn, bool isInSession)
        {
            buttonDeleteSetup.IsEnabled = !isBuiltIn && !isInSession;
            buttonRenameSetup.IsEnabled = !isBuiltIn && !isInSession;
        }

        /// <summary>
        /// Helper method to convert CheckBox.IsChecked to usable bool.
        /// </summary>
        /// <param name="checkBox">The check box.</param>
        /// <returns>True if the box is checked, false if unchecked or uninitialized.</returns>
        private bool GetCheckbuttonChecked(CheckBox checkBox)
        {
            if (checkBox.IsChecked.HasValue)
                return checkBox.IsChecked.Value;
            return false;
        }

        /// <summary>
        /// Helper method to convert RadioButton.IsChecked to usable bool.
        /// </summary>
        /// <param name="checkBox">The check box.</param>
        /// <returns>True if the box is checked, false if unchecked or uninitialized.</returns>
        private bool GetRadiobuttonChecked(RadioButton checkBox)
        {
           if (checkBox.IsChecked.HasValue)
              return checkBox.IsChecked.Value;
           return false;
        }
        /// <summary>
        /// The OK button callback.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            // close the window
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Cancel button callback.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            // close the window
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Remove a configuration from the listbox and the map.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void buttonDeleteSetup_Click(object sender, RoutedEventArgs e)
        {
            IFCExportConfiguration configuration = (IFCExportConfiguration)listBoxConfigurations.SelectedItem;
            m_configurationsMap.Remove(configuration.Name);
            listBoxConfigurations.Items.Remove(configuration);
            listBoxConfigurations.SelectedIndex = 0;
        }

        /// <summary>
        /// Shows the rename control and updates with the results.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void buttonRenameSetup_Click(object sender, RoutedEventArgs e)
        {
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            String oldName = configuration.Name;
            RenameExportSetupWindow renameWindow = new RenameExportSetupWindow(m_configurationsMap, oldName);
            renameWindow.Owner = this;
            renameWindow.ShowDialog();
            if (renameWindow.DialogResult.HasValue && renameWindow.DialogResult.Value)
            {
                String newName = renameWindow.GetName();
                configuration.Name = newName;
                m_configurationsMap.Remove(oldName);
                m_configurationsMap.Add(configuration);
                UpdateConfigurationsList(newName);
            }
        }

        /// <summary>
        /// Shows the duplicate control and updates with the results.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void buttonDuplicateSetup_Click(object sender, RoutedEventArgs e)
        {
            String name = GetDuplicateSetupName(null);
            NewExportSetupWindow nameWindow = new NewExportSetupWindow(m_configurationsMap, name);
            nameWindow.Owner = this;
            nameWindow.ShowDialog();
            if (nameWindow.DialogResult.HasValue && nameWindow.DialogResult.Value)
            {
                CreateNewEditableConfiguration(GetSelectedConfiguration(), nameWindow.GetName());
            }
        }

        /// <summary>
        /// Shows the new setup control and updates with the results.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void buttonNewSetup_Click(object sender, RoutedEventArgs e)
        {
            String name = GetNewSetupName();
            NewExportSetupWindow nameWindow = new NewExportSetupWindow(m_configurationsMap, name);
            nameWindow.Owner = this;
            nameWindow.ShowDialog();
            if (nameWindow.DialogResult.HasValue && nameWindow.DialogResult.Value)
            {
                CreateNewEditableConfiguration(null, nameWindow.GetName());
            }
        }

        /// <summary>
        /// Gets the new setup name.
        /// </summary>
        /// <returns>The new setup name.</returns>
        private String GetNewSetupName()
        {
            return GetFirstIncrementalName("Setup");
        }

        /// <summary>
        /// Gets the new duplicated setup name.
        /// </summary>
        /// <param name="configuration">The configuration to duplicate.</param>
        /// <returns>The new duplicated setup name.</returns>
        private String GetDuplicateSetupName(IFCExportConfiguration configuration)
        {
            if (configuration == null)
                configuration = GetSelectedConfiguration();
            return GetFirstIncrementalName(configuration.Name);
        }

        /// <summary>
        /// Gets the new incremental name for configuration.
        /// </summary>
        /// <param name="nameRoot">The name of a configuration.</param>
        /// <returns>the new incremental name for configuration.</returns>
        private String GetFirstIncrementalName(String nameRoot)
        {
            bool found = true;
            int number = 0;
            String newName = "";
            do
            {
                number++;
                newName = nameRoot + " " + number;
                if (!m_configurationsMap.HasName(newName))
                    found = false;
            }
            while (found);

            return newName;
        }

        

        /// <summary>
        /// Creates a new configuration, either a default or a copy configuration.
        /// </summary>
        /// <param name="configuration">The specific configuration, null to create a defult configuration.</param>
        /// <param name="name">The name of the new configuration.</param>
        /// <returns>The new configuration.</returns>
        private IFCExportConfiguration CreateNewEditableConfiguration(IFCExportConfiguration configuration, String name)
        {
            // create new configuration based on input, or default configuration.
            IFCExportConfiguration newConfiguration;
            if (configuration == null)
            {
                newConfiguration = IFCExportConfiguration.CreateDefaultConfiguration();
                newConfiguration.Name = name;
            }
            else
                newConfiguration = configuration.Duplicate(name);
            m_configurationsMap.Add(newConfiguration);

            // set new configuration as selected
            listBoxConfigurations.Items.Add(newConfiguration);
            listBoxConfigurations.SelectedItem = newConfiguration;

            return configuration;
        }

        /// <summary>
        /// Gets the selected configuration from the list box.
        /// </summary>
        /// <returns>The selected configuration.</returns>
        private IFCExportConfiguration GetSelectedConfiguration()
        {
            IFCExportConfiguration configuration = (IFCExportConfiguration)listBoxConfigurations.SelectedItem;
            return configuration;
        }

        /// <summary>
        /// Gets the name of selected configuration.
        /// </summary>
        /// <returns>The selected configuration name.</returns>
        public String GetSelectedConfigurationName()
        {
            return GetSelectedConfiguration().Name;
        }

        /// <summary>
        /// Updates the controls after listbox selection changed.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void listBoxConfigurations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            if (configuration != null)
            {
                UpdateActiveConfigurationOptions(configuration);
                UpdateConfigurationControls(configuration.IsBuiltIn, configuration.IsInSession);
            }
        }

        /// <summary>
        /// Updates the result after the ExportBaseQuantities is picked.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void checkboxExportBaseQuantities_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            if (configuration != null)
            {
                configuration.ExportBaseQuantities = GetCheckbuttonChecked(checkBox);
            }
        }

        /// <summary>
        /// Updates the result after the SplitWalls is picked.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void checkboxSplitWalls_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            if (configuration != null)
            {
                configuration.SplitWallsAndColumns = GetCheckbuttonChecked(checkBox);
            }
        }

        /// <summary>
        /// Updates the result after the InternalPropertySets is picked.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void checkboxInternalPropertySets_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            if (configuration != null)
            {
                configuration.ExportInternalRevitPropertySets = GetCheckbuttonChecked(checkBox);
            }
        }

        /// <summary>
        /// Updates the result after the InternalPropertySets is picked.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void checkboxIFCCommonPropertySets_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            if (configuration != null)
            {
                configuration.ExportIFCCommonPropertySets = GetCheckbuttonChecked(checkBox);
            }
        }

        /// <summary>
        /// Updates the result after the 2dElements is picked.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void checkbox2dElements_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            if (configuration != null)
            {
                configuration.Export2DElements = GetCheckbuttonChecked(checkBox);
            }
        }

        /// <summary>
        /// Updates the result after the VisibleElementsCurrView is picked.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void checkboxVisibleElementsCurrView_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            if (configuration != null)
            {
                configuration.VisibleElementsOfCurrentView = GetCheckbuttonChecked(checkBox);
                if (!configuration.VisibleElementsOfCurrentView)
                {
                    configuration.ExportPartsAsBuildingElements = false;
                    checkBoxExportPartsAsBuildingElements.IsChecked = false;
                    comboboxActivePhase.IsEnabled = true;
                    checkBoxExportRoomsInView.IsEnabled = false;
                    checkBoxExportRoomsInView.IsChecked = false;
                }
                else
                {
                    checkBoxExportRoomsInView.IsEnabled = true;
                    UpdatePhaseAttributes(configuration);
                }
            }
        }

        /// <summary>
        /// Updates the result after the Use2DRoomVolumes is picked.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void checkBoxUse2DRoomVolumes_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            if (configuration != null)
            {
                configuration.Use2DRoomBoundaryForVolume = GetCheckbuttonChecked(checkBox);
            }
        }

        /// <summary>
        /// Updates the result after the FamilyAndTypeName is picked.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void checkBoxFamilyAndTypeName_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            if (configuration != null)
            {
                configuration.UseFamilyAndTypeNameForReference = GetCheckbuttonChecked(checkBox);
            }
        }

        /// <summary>
        /// Updates the configuration IFCVersion when IFCType changed in the combobox.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void comboboxIfcType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IFCVersionAttributes attributes = (IFCVersionAttributes)comboboxIfcType.SelectedItem;
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            if (configuration != null)
            {
                configuration.IFCVersion = attributes.Version;
            }
        }

        /// <summary>
        /// Updates the configuration IFCFileType when FileType changed in the combobox.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void comboboxFileType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IFCFileFormatAttributes attributes = (IFCFileFormatAttributes)comboboxFileType.SelectedItem;
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            if (configuration != null)
            {
                configuration.IFCFileType = attributes.FileType;
            }
        }

        /// <summary>
        /// Updates the configuration SpaceBoundaries when the space boundary level changed in the combobox.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void comboboxSpaceBoundaries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IFCSpaceBoundariesAttributes attributes = (IFCSpaceBoundariesAttributes)comboboxSpaceBoundaries.SelectedItem;
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            if (configuration != null)
            {
                configuration.SpaceBoundaries = attributes.Level;
            }
        }

        /// <summary>
        /// Updates the configuration ActivePhase when the active phase changed in the combobox.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void comboboxActivePhase_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IFCPhaseAttributes attributes = (IFCPhaseAttributes)comboboxActivePhase.SelectedItem;
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            if (configuration != null)
            {
                configuration.ActivePhaseId = attributes.PhaseId;
            }
        }
        
        /// <summary>
        /// Saves the window bounds when close the window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save restore bounds for the next time this window is opened
            IFCUISettings.SaveWindowBounds(m_SettingFile, this.RestoreBounds);
        }

        /// <summary>
        /// Updates the configuration ExportPartsAsBuildingElements when the Export separate parts changed in the combobox.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void checkBoxExportPartsAsBuildingElements_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            if (configuration != null)
            {
                configuration.ExportPartsAsBuildingElements = GetCheckbuttonChecked(checkBox);
                if (configuration.ExportPartsAsBuildingElements)
                {
                    configuration.VisibleElementsOfCurrentView = true;
                    checkboxVisibleElementsCurrView.IsChecked = true;
                }
            }
        }

        private void checkBoxUseActiveViewGeometry_Checked(object sender, RoutedEventArgs e)
        {
           CheckBox checkBox = (CheckBox)sender;
           IFCExportConfiguration configuration = GetSelectedConfiguration();
           if (configuration != null)
           {
              configuration.UseActiveViewGeometry = GetCheckbuttonChecked(checkBox);
           }
        }
       
       /// <summary>
        /// Updates the configuration ExportBoundingBox when the Export Bounding Box changed in the check box.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void checkboxExportBoundingBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            if (configuration != null)
            {
                configuration.ExportBoundingBox = GetCheckbuttonChecked(checkBox);
            }
        }

        /// <summary>
        /// Updates the configuration ExportSolidModelRep when the "Export Solid Models when Possible" option changed in the check box.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void checkboxExportSolidModelRep_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            if (configuration != null)
            {
                configuration.ExportSolidModelRep = GetCheckbuttonChecked(checkBox);
            }
        }

        /// <summary>
        /// Updates the configuration ExportSchedulesAsPsets when the "Export schedules as property sets" option changed in the check box.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void checkboxExportSchedulesAsPsets_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            if (configuration != null)
            {
                configuration.ExportSchedulesAsPsets = GetCheckbuttonChecked(checkBox);
            }
        }

        /// <summary>
        /// Updates the configuration IncludeSiteElevation when the Export Bounding Box changed in the check box.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void checkboxIfcSiteElevation_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            if (configuration != null)
            {
                configuration.IncludeSiteElevation = GetCheckbuttonChecked(checkBox);
            }
        }

        /// <summary>
        /// Updates the configuration StoreIFCGUID when the Store IFC GUID changed in the check box.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void checkboxStoreIFCGUID_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            if (configuration != null)
            {
                configuration.StoreIFCGUID = GetCheckbuttonChecked(checkBox);
            }
        }

        private void checkBoxExportSpecificSchedules_Checked(object sender, RoutedEventArgs e)
        {
           CheckBox checkBox = (CheckBox)sender;
           IFCExportConfiguration configuration = GetSelectedConfiguration();
           if (configuration != null)
           {
              configuration.ExportSpecificSchedules = GetCheckbuttonChecked(checkBox);
              if ((bool)configuration.ExportSpecificSchedules)
              {
                 configuration.ExportSchedulesAsPsets = true;
                 checkboxExportSchedulesAsPsets.IsChecked = true;
              }
           }
        }

        /// <summary>
        /// Update checkbox for user-defined Pset option
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void checkboxExportUserDefinedPset_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            if (configuration != null)
            {
                configuration.ExportUserDefinedPsets = GetCheckbuttonChecked(checkBox);
                userDefinedPropertySetFileName.IsEnabled = configuration.ExportUserDefinedPsets;
                buttonBrowse.IsEnabled = configuration.ExportUserDefinedPsets;
            }
        }

        /// <summary>
        /// Update checkbox for export linked files option
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void checkBoxExportLinkedFiles_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            if (configuration != null)
            {
                configuration.ExportLinkedFiles = GetCheckbuttonChecked(checkBox);
            }
        }

        /// <summary>
        /// Shows the new setup control and updates with the results.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments that contains the event data.</param>
        private void buttonBrowse_Click(object sender, RoutedEventArgs e)
        {
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".txt";
            dlg.Filter = Properties.Resources.UserDefinedParameterSets + @"|*.txt";
            if (configuration != null && !string.IsNullOrWhiteSpace(configuration.ExportUserDefinedPsetsFileName))
            {
                string pathName = System.IO.Path.GetDirectoryName(configuration.ExportUserDefinedPsetsFileName);
                if (Directory.Exists(pathName))
                    dlg.InitialDirectory = pathName;
                if (File.Exists(configuration.ExportUserDefinedPsetsFileName))
                {
                    string fileName = System.IO.Path.GetFileName(configuration.ExportUserDefinedPsetsFileName);
                    dlg.FileName = fileName;
                }
            }

            // Display OpenFileDialog by calling ShowDialog method 
            bool? result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result.HasValue && result.Value)
            {
                string filename = dlg.FileName;
                userDefinedPropertySetFileName.Text = filename;
                if (configuration != null)
                    configuration.ExportUserDefinedPsetsFileName = filename;
            }
        }

        private void checkBoxExportRoomsInView_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            if (configuration != null)
            {
                configuration.ExportRoomsInView = GetCheckbuttonChecked(checkBox);
            }
        }

        private void comboBoxLOD_SelectionChanged(object sender, RoutedEventArgs e)
        {
            string selectedItem = (string)comboBoxLOD.SelectedItem;
            IFCExportConfiguration configuration = GetSelectedConfiguration();
            if (configuration != null)
            {
                double levelOfDetail = 0;
                if (string.Compare(selectedItem, Properties.Resources.DetailLevelExtraLow) == 0)
                    levelOfDetail = 0.25;
                else if (string.Compare(selectedItem, Properties.Resources.DetailLevelLow) == 0)
                    levelOfDetail = 0.5;
                else if (string.Compare(selectedItem, Properties.Resources.DetailLevelMedium) == 0)
                    levelOfDetail = 0.75;
                else
                    // detail level is high
                    levelOfDetail = 1;
                configuration.TessellationLevelOfDetail = levelOfDetail;
            }
        }
    }
}
