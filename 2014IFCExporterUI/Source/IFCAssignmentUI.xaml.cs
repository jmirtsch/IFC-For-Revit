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

using System;
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
using System.Windows.Shapes;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

using Revit.IFC.Common.Extensions;


namespace BIM.IFC.Export.UI
{
    /// <summary>
    /// Interaction logic for File Header Tab in IFCAssignmentUI.xaml
    /// </summary>
    public partial class IFCAssignment : Window
    {
        private string[] ifcPurposeList = { "OFFICE", "SITE", "HOME", "DISTRIBUTIONPOINT", "USERDEFINED" };
        private string[] purposeList = 
        { 
            Properties.Resources.Office, 
            Properties.Resources.Site,
            Properties.Resources.Home, 
            Properties.Resources.DistributionPoint,
            Properties.Resources.UserDefined 
        };
    
        private IFCAddress m_newAddress = new IFCAddress();
        private IFCFileHeader m_newFileHeader = new IFCFileHeader();
        private IFCAddressItem m_newAddressItem = new IFCAddressItem();
        private IFCAddressItem m_savedAddressItem = new IFCAddressItem();
        private IFCFileHeaderItem m_newFileHeaderItem = new IFCFileHeaderItem();
        private IFCFileHeaderItem m_savedFileHeaderItem = new IFCFileHeaderItem();
        private IFCClassification m_newClassification = new IFCClassification();
        private IList<IFCClassification> m_newClassificationList = new List<IFCClassification>();
        private IFCClassification m_savedClassification = new IFCClassification();

        private string getUserDefinedStringFromIFCPurposeList()
        {
            return ifcPurposeList[4];
        }

        /// <summary>
        /// initialization of IFCAssignemt class
        /// </summary>
        /// <param name="document"></param>
        public IFCAssignment()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Event when Window is loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AddressTab.DataContext = m_newAddressItem;
            FileHeaderTab.DataContext = m_newFileHeaderItem;
            ClassificationTab.DataContext = m_newClassification;
        }

        /// <summary>
        /// Event when the Purpose combo box is initialized. The list of enum text for Purpose is added here
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PurposeComboBox_Initialized(object sender, EventArgs e)
        {
            foreach (string currPurpose in purposeList)
            {
                PurposeComboBox.Items.Add(currPurpose);
            }
        }

        /// <summary>
        /// Event when selection of combo box item is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PurposeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            m_newAddressItem.Purpose = ifcPurposeList[PurposeComboBox.SelectedIndex];
            if (String.Compare(m_newAddressItem.Purpose, getUserDefinedStringFromIFCPurposeList()) != 0) // ifcPurposeList == "USERDEFINED"
                m_newAddressItem.UserDefinedPurpose = "";         // Set User Defined Purpose field to empty if the Purpose is changed to other values
        }

        /// <summary>
        /// Event when OK button is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void buttonOK_Click(object sender, RoutedEventArgs args)
        {

            // Saved changes to both Address Tab items and File Header Tab items if they have changed

            if (m_newAddressItem.isUnchanged(m_savedAddressItem) == false)
            {
                m_newAddress.UpdateAddress(IFCCommandOverrideApplication.TheDocument, m_newAddressItem);
            }

            if (m_newFileHeaderItem.isUnchanged(m_savedFileHeaderItem) == false)
            {
                m_newFileHeader.UpdateFileHeader(IFCCommandOverrideApplication.TheDocument, m_newFileHeaderItem);
            }

            if (m_newAddressItem.UpdateProjectInformation == true)
            {
                // Format IFC address and update it into Project Information: Project Address
                String address = null;
                String geographicMapLocation = null;
                bool addNewLine = false;

                if (String.IsNullOrEmpty(m_newAddressItem.AddressLine1) == false)
                    address += string.Format("{0}\r\n", m_newAddressItem.AddressLine1);
                if (String.IsNullOrEmpty(m_newAddressItem.AddressLine2) == false)
                    address += string.Format("{0}\r\n", m_newAddressItem.AddressLine2);
                if (String.IsNullOrEmpty(m_newAddressItem.POBox) == false)
                    address += string.Format("{0}\r\n", m_newAddressItem.POBox);
                if (String.IsNullOrEmpty(m_newAddressItem.TownOrCity) == false)
                {
                    address += string.Format("{0}", m_newAddressItem.TownOrCity);
                    addNewLine = true;
                }
                if (String.IsNullOrEmpty(m_newAddressItem.RegionOrState) == false)
                {
                    address += string.Format(", {0}", m_newAddressItem.RegionOrState);
                    addNewLine = true;
                }
                if (String.IsNullOrEmpty(m_newAddressItem.PostalCode) == false)
                {
                    address += string.Format(" {0}", m_newAddressItem.PostalCode);
                    addNewLine = true;
                }
                if (addNewLine == true)
                {
                    address += string.Format("\r\n");
                    addNewLine = false;
                }

                if (String.IsNullOrEmpty(m_newAddressItem.Country) == false)
                    address += string.Format("{0}\r\n", m_newAddressItem.Country);

                if (String.IsNullOrEmpty(m_newAddressItem.InternalLocation) == false)
                    address += string.Format("\r\n{0}: {1}\r\n", 
                        Properties.Resources.InternalAddress, m_newAddressItem.InternalLocation);

                if (String.IsNullOrEmpty(m_newAddressItem.TownOrCity) == false)
                    geographicMapLocation = m_newAddressItem.TownOrCity;

                if (String.IsNullOrEmpty(m_newAddressItem.RegionOrState) == false)
                {
                    if (String.IsNullOrEmpty(geographicMapLocation) == false)
                        geographicMapLocation = geographicMapLocation + ", " + m_newAddressItem.RegionOrState;
                    else
                        geographicMapLocation = m_newAddressItem.RegionOrState;
                }

                if (String.IsNullOrEmpty(m_newAddressItem.Country) == false)
                {
                    if (String.IsNullOrEmpty(geographicMapLocation) == false)
                        geographicMapLocation = geographicMapLocation + ", " + m_newAddressItem.Country;
                    else
                        geographicMapLocation = m_newAddressItem.Country;
                };

                Transaction transaction = new Transaction(IFCCommandOverrideApplication.TheDocument, Properties.Resources.UpdateProjectAddress);
                transaction.Start();

                ProjectInfo projectInfo = IFCCommandOverrideApplication.TheDocument.ProjectInformation;
                projectInfo.Address = address;    // set project address information using the IFC Address Information when requested

                if (String.IsNullOrEmpty(geographicMapLocation) == false)
                    IFCCommandOverrideApplication.TheDocument.ActiveProjectLocation.SiteLocation.PlaceName = geographicMapLocation;    // Update also Revit Site location on the Map using City, State and Country when they are not null

                transaction.Commit();
            }

            // Update Classification if it has changed or the mandatory fields are filled. If mandatory fields are not filled we will ignore the classification.
            if (!m_newClassification.IsUnchanged(m_savedClassification))
            {
                if (m_newClassification.AreMandatoryFieldsFilled())
                {
                    IFCClassificationMgr.UpdateClassification(IFCCommandOverrideApplication.TheDocument, m_newClassification);
                }
                else if (!m_newClassification.IsClassificationEmpty())
                {
                    m_newClassification.ClassificationTabMsg = Properties.Resources.ManditoryFieldsNotEmpty;
                    return;
                }
            }

            m_newClassification.ClassificationTabMsg = null;
            Close();
        }

        /// <summary>
        /// Event when Cancel button is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bottonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Event when update project information is checked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateProjInfocheckBox_Checked(object sender, RoutedEventArgs e)
        {
            m_newAddressItem.UpdateProjectInformation = true;
        }

        /// <summary>
        /// Event when the update project information is unchecked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateProjInfocheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            m_newAddressItem.UpdateProjectInformation = false;
        }

        /// <summary>
        /// Event when Tab control is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AssignmenttabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool status = classificationTabMandatoryItemCheck(sender, e);
        }

        /// <summary>
        /// Checking Mandatory fields are being filled otherwise do not allow user to leave the Tab
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private Boolean classificationTabMandatoryItemCheck(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0)
            {
                TabItem tItemRemoved = e.RemovedItems[0] as TabItem;
                TabItem tItemAdded = e.AddedItems[0] as TabItem;

                if (tItemRemoved != null && tItemAdded != null)  // skip if it is not a TabItem type
                {
                    if (String.Compare(tItemRemoved.Header.ToString(), Properties.Resources.Classification) == 0 && 
                        tItemAdded != tItemRemoved)  // avoid loop when we force the Tab back to the same one
                    {
                        // Current tab item is Classification Tab
                        if (!m_newClassification.IsClassificationEmpty()) // we will skip the mandatory field check when all fields are empty (i.e. user does not intend to create any Classification)
                        {
                            if (!m_newClassification.AreMandatoryFieldsFilled())
                            {
                                m_newClassification.ClassificationTabMsg = Properties.Resources.ManditoryFieldsNotEmpty;
                                AssignmenttabControl.SelectedItem = tItemRemoved;  // Force the tab to return to Classification
                                return false;
                            }
                            else
                                m_newClassification.ClassificationTabMsg = null;        // reset the message
                        }
                        else
                            m_newClassification.ClassificationTabMsg = null;        // reset the message
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Upon AddressTab initialization
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddressTab_Initialized(object sender, EventArgs e)
        {
            bool hasSavedItem = m_newAddress.GetSavedAddress(IFCCommandOverrideApplication.TheDocument, out m_newAddressItem);
            if (hasSavedItem == true)
            {
                //keep a copy of the original saved items for checking for any value changed later on
                m_savedAddressItem = m_newAddressItem.Clone();

                // We won't initialize PurposeComboBox.SelectedIndex, as otherwise that will change the value
                // of m_newAddressItem.Purpose to the first item in the list, which we don't want.   It is
                // OK for this to be "uninitialized".

                // This is a short list, so we just do an O(n) search.
                int numItems = ifcPurposeList.Count();
                for (int ii = 0; ii < numItems; ii++)
                {
                    if (m_newAddressItem.Purpose == ifcPurposeList[ii])
                    {
                        PurposeComboBox.SelectedIndex = ii;
                        break;
                    }
                }
            }
            else
            {
                string projLocation = IFCCommandOverrideApplication.TheDocument.SiteLocation.PlaceName;
                if (projLocation != null)
                {
                    // if the formatted Address is empty and there is location set for the site (project) in Revit, take it and assign it by default to AddressLine1.
                    m_newAddressItem.AddressLine1 = projLocation;
                }
            }
        }

        /// <summary>
        /// Initialization of File Header tab
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileHeaderTab_Initialized(object sender, EventArgs e)
        {
            bool hasSavedItem = m_newFileHeader.GetSavedFileHeader(IFCCommandOverrideApplication.TheDocument, out m_newFileHeaderItem);
            if (hasSavedItem == true)
            {
                m_savedFileHeaderItem = m_newFileHeaderItem.Clone();
            }

            // File Description and Source File name are assigned by exporter later on and therefore needs to be set to null for the UI for the null value text to be displayed
            m_newFileHeaderItem.FileDescription = null;
            m_newFileHeaderItem.SourceFileName = null;
            m_newFileHeaderItem.FileSchema = null;

            // Application Name and Number are fixed for the software release and will not change, therefore they are always forced set here
            m_newFileHeaderItem.ApplicationName = IFCCommandOverrideApplication.TheDocument.Application.VersionName;
            m_newFileHeaderItem.VersionNumber = IFCCommandOverrideApplication.TheDocument.Application.VersionBuild;

        }

        /// <summary>
        /// Update the Purpose Combo Box value to USERDEFINED when the value is set to something else 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserDefinedPurposeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(this.m_newAddressItem.UserDefinedPurpose) == false)
            {
                m_newAddressItem.Purpose = getUserDefinedStringFromIFCPurposeList();
                PurposeComboBox.SelectedItem = Properties.Resources.UserDefined;
            }
        }


        /// <summary>
        /// Initialization of the Classification Tab when there is saved item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClassificationTab_Initialized(object sender, EventArgs e)
        {
            bool hasSavedItem = IFCClassificationMgr.GetSavedClassifications(IFCCommandOverrideApplication.TheDocument, null, out m_newClassificationList);
            m_newClassification = m_newClassificationList[0];                        // Set the default first Classification item to the first member of the List

            if (hasSavedItem == true)
            {
                m_savedClassification = m_newClassification.Clone();
            }

            if (m_newClassification.ClassificationEditionDate <= DateTime.MinValue || m_newClassification.ClassificationEditionDate >= DateTime.MaxValue)
            {
                DateTime today = DateTime.Now;
                m_newClassification.ClassificationEditionDate = today;
            }
        }
    }
}

