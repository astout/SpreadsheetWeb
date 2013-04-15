﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SS;
using System.IO;
using System.Text.RegularExpressions;

namespace SpreadsheetClient
{
    public partial class SpreadsheetGUI : Form
    {

        private Spreadsheet ss;

        //class wide variables to track whether a file has been saved
        //Mostly to avoid repeated Save Dialogs and for 
        //Save as functionality.
        private bool UpToDate;
        private string savedName;

        /// <summary>
        /// The spreadsheet window
        /// </summary>
        public SpreadsheetGUI()
        {
            InitializeComponent();

            //create a new spreadsheet, save the version as ps6
            ss = new Spreadsheet(IsValid, Normalize, "ps6"); 
            //initialize the selection to cell A1
            ssp.SetSelection(0, 0);
            //if the Enter key is struck, assume user means Set Cell Contents
            AcceptButton = btn_SetContents;
            RefreshTextFields(ssp); //refresh the text fields at the top
            UpToDate = true; //this file has not been saved before
            savedName = ""; //thus it has no filename
        }


        /// <summary>
        /// This is the method to pass as a delegate for a valid cell name.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public bool IsValid(string s)
        {
            string alphaNum = @"([a-zA-Z]+)([1-9]?[0-9]{1}$|^99$/)";
            if (Regex.IsMatch(s, alphaNum))
                return true;
            else
                return false;
        }


        /// <summary>
        /// this is the method that normalizes each cell name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string Normalize(string name)
        {
            return name.ToUpper();
        }
        

        /// <summary>
        /// if the selection changes, refresh the textfields
        /// </summary>
        /// <param name="sender"></param>
        private void spreadsheetPanel1_SelectionChanged(SS.SpreadsheetPanel sender)
        {
            RefreshTextFields(sender);
        }


        /// <summary>
        /// This method refreshes the text boxes at the top of the window to the appropriate values
        /// of each cell.
        /// </summary>
        /// <param name="sender"></param>
        private void RefreshTextFields(SpreadsheetPanel sender)
        {
            //set the Cell Address Text Field according to the coordinates of the SpreadsheetPanel
            int col, row;
            ssp.GetSelection(out col, out row);
            txtBox_Cell.Text = SetAddress(col, row);

            //Set the Value Text Field to display FormulaError if there's an error
            if(ss.GetCellValue(txtBox_Cell.Text).GetType() == typeof(SpreadsheetUtilities.FormulaError))
                txtBox_Value.Text = "Formula Error";
            //otherwise display the valid value of the cell    
            else
                txtBox_Value.Text = ss.GetCellValue(txtBox_Cell.Text).ToString();

            //if the contents type is a formula, display it with an '=' appended
            if(ss.GetCellContents(txtBox_Cell.Text).GetType() == typeof(SpreadsheetUtilities.Formula))
                txtBox_Contents.Text = "=" + ss.GetCellContents(txtBox_Cell.Text).ToString();
            //otherwise display the contents of the cell
            else
                txtBox_Contents.Text = ss.GetCellContents(txtBox_Cell.Text).ToString();

            //place the focus back on the contents text field
            txtBox_Contents.Focus();
        }


        /// <summary>
        /// This is the action of clicking the 'Set Contents' button.
        /// It sets the contents of the selected cell.  The value is set according to the contents.
        /// It ensures that the appropriate information is displayed in each field and in the SpreadsheetPanel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_SetContents_Click(object sender, EventArgs e)
        {
            //Grab the name, address, and coordinates
            string contents;
            //if the content is a formula
            if (txtBox_Contents.Text.Trim().StartsWith("="))
                contents = txtBox_Contents.Text.Trim().ToUpper(); //set the text to upper case
            else
                contents = txtBox_Contents.Text.Trim(); //otherwise, set it to the text as is
            string name = txtBox_Cell.Text.Trim();

            //address is the numeric coordinates of the selected cell
            int[] address = GetCoordinates(name);

            //set the contents of the spreadsheet to what the user enters
            try
            {
                IEnumerable<string> dependees = ss.SetContentsOfCell(name, contents);
                foreach (string s in dependees)
                {
                    address = GetCoordinates(s);
                    object v = ss.GetCellValue(s);
                    RefreshTextFields(ssp);
                    //if it returns a formula error, display that in a more friendly way.
                    if (v.GetType() == typeof(SpreadsheetUtilities.FormulaError))
                        ssp.SetValue(address[0], address[1] - 1, "Formula Error");
                    else
                        ssp.SetValue(address[0], address[1] - 1, v.ToString());
                }
                //Set the text fields
                FillSpreadSheet(ssp, ss);
            }
            catch (Exception x)
            {
                if(x is CircularException)
                    MessageBox.Show("The entered formula would cause a circular dependency.");
                else if (x is SpreadsheetUtilities.FormulaFormatException)
                    MessageBox.Show("An invalid formula was entered.");
                else if (x is NullReferenceException)
                    MessageBox.Show("An invalid formula was entered.");
                else
                    throw x;
            }

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menu_File_New_Click(object sender, EventArgs e)
        {
            SpreadsheetApplicationContext.getAppContext().RunForm(new SpreadsheetGUI());
        }

        /// <summary>
        /// This method executes the Form is asked to close.  It checks for unsaved changes and prompts 
        /// the user appropriately.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, CancelEventArgs e)
        {
            //the spreadsheet has changed, warn the user.
            if (ss.Changed)
                DialogResult = MessageBox.Show("Save Changes? If you say no, Changes will be lost.", "Changes Have Been Made",
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            switch (DialogResult)
            {
                case DialogResult.Cancel:
                    e.Cancel = true;
                    return;

                case DialogResult.Yes:
                    menu_File_Save_Click(sender, e);
                    break;

                case DialogResult.No:
                    break;
            }
        }


        /// <summary>
        /// The method saves the file to the location chosen by the user.
        /// If the file has been previously saved, just overwrites that file.
        /// Recommends using '.ss' file extensions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menu_File_Save_Click(object sender, EventArgs e)
        {
            if (!UpToDate)
            {
                //If the client's copy isn't up to date, they can't commit a save
            }
            else
                ss.Save(savedName); //if the client is presumed up to date, attempt a save.
        }


        /// <summary>
        /// close appropriately if the user selects Exit from the file menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menu_File_Exit_Click(object sender, EventArgs e)
        {
            Close();
        }


        /// <summary>
        /// Opens a saved file and Displays the spreadsheet.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menu_File_Open_Click(object sender, EventArgs e)
        {
            Stream stream; //used for initializing the new window.
            OpenFileDialog fileBrowser = new OpenFileDialog(); //new file browser window
            fileBrowser.Filter = "SimpleSheet files (*.ss)|*.ss|All Files (*.*)|*.*"; //filter to .ss or All files.
            if (fileBrowser.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if ((stream = fileBrowser.OpenFile()) != null)
                    {
                        using (stream)
                        {
                            SpreadsheetGUI newForm = new SpreadsheetGUI(); //create a new window
                            //fill a spreadsheet using the saved file.
                            newForm.ss = new Spreadsheet(fileBrowser.FileName, IsValid, Normalize, "ps6");
                            //run the new window
                            SpreadsheetApplicationContext.getAppContext().RunForm(newForm);
                            //fill the spreadsheet panel
                            FillSpreadSheet(newForm.ssp, newForm.ss);
                            //note this is a saved file
                            newForm.UpToDate = true;
                            //store the filename
                            newForm.savedName = fileBrowser.FileName;
                        }
                    }
                }
                catch (Exception x)
                {
                    if (x is IOException)
                        MessageBox.Show("There was a problem reading the file", "File Read Error");
                    else if (x is SpreadsheetReadWriteException)
                        MessageBox.Show(x.Message);
                }

            }
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Send UNDO Protocol
        }
        

        /// <summary>
        /// This displays a simple message box that displays how to use the SimpleSheets application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void showHelpDocumentationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String text = "";
            text += "To change the contents of a cell, highlight the cell, type into the \"Contents\" textbox, ";
            text += "then either hit the enter key, or click the \"Set Contents\" button. \n\n";
            text += "The contents of a cell may be text as a string, a formula (i.e. = 6 + 6, =A2*C4), or a number. \n\n";
            text += "The \"Cell\" text box shows you what the address of the selected cell is (i.e. A1 or Z99)\n\n";
            text += "The \"Value\" text box shows you the value that is displayed in the selected cell.\n";
            text += "Value is evaluated based on the contents of the cell (i.e. A1 contents = \"=2+2\"; A1 value = \"4\"\n\n";
            text += "To open a previously saved file, click File -> Open and choose the file you wish to open from the dialog.\n\n";
            text += "To save a file, click File -> Save, give the file a name and location in the dialog window, and click Save.\n\n";
            text += "Note: This application prefers you to open and save files in the .ss format.\n\n";
            text += "To close a window, click File -> Close, click the Red 'X' in the top right corner.\n";
            text += "If the file you are trying to close has any changes, you will be warned and asked if you want to save.\n";
            MessageBox.Show(text, "Help Documentation");
        }


        /*
         * 
         * Tools
         * 
         * 
         **/

        /// <summary>
        /// Get Column coordinate as an int from a char.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static int GetColumnCoordinate(char c)
        {
            return (int)(char.ToUpper(c)) - 65;
        }


        /// <summary>
        /// Set the coordinate as a char from an int.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static char SetColumnCoordinate(int i)
        {
            return char.ToUpper((char)(i + 65));
        }


        /// <summary>
        /// Returns an array containing the coordinates of the
        /// specified cell.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>int[] coordinates</returns>
        public static int[] GetCoordinates(String name)
        {
            int[] coords = new int[2]; //new int array of size 2
            char c = name[0]; //set the char to the first element of the cell name
            coords[0] = GetColumnCoordinate(c); //translate the char to a number
            Int32.TryParse(name.Substring(1), out coords[1]); //get the row from the string

            return coords; //return the coordinates
        }


        /// <summary>
        /// Sets the address as a string of the given column and row.
        /// </summary>
        /// <param name="col"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        public static string SetAddress(int col, int row)
        {
            return "" + SetColumnCoordinate(col) + (row + 1);
        }


        /// <summary>
        /// This fills the entire spreadsheet panel according the passed spreadsheet.
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="savedSheet"></param>
        private void FillSpreadSheet(SpreadsheetPanel panel, Spreadsheet savedSheet)
        {
            //what do we need to update?
            IEnumerable<string> names = savedSheet.GetNamesOfAllNonemptyCells();
            
            //update each cell that contains data
            foreach (string s in names)
            {
                int[] address = GetCoordinates(s);
                object v = savedSheet.GetCellValue(s);
                if (v.GetType() == typeof(SpreadsheetUtilities.FormulaError))
                    panel.SetValue(address[0], address[1] - 1, "Formula Error");
                else
                    panel.SetValue(address[0], address[1] - 1, v.ToString());
            }
        }

       

    }
}