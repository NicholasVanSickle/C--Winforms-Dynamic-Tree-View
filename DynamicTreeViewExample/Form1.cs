using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using DynamicTreeView;

namespace DynamicTreeViewExample
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            treeView.Nodes.Add("Text Node");
            var parent = treeView.Nodes.Add("Parent Node");
            parent.Nodes.Add("Normal Child");
            parent.Nodes.Add("Multiline\nChild");
            parent.Nodes.Add("\fcFF0000Colored\fc Child \fc00FF00Node\fc");
        }

        private void treeView_NodeMouseDown(object sender, DynamicTreeNodeMouseEventArgs e)
        {
            DynamicTreeNode selection = e.Node;

            if (selection == null && e.Button == MouseButtons.Right)
            {
                //select regardless of horizontal alignment in the view                
                selection =
                    treeView.Nodes.VisibleNodes.Where(node => node.Bounds.Top <= e.Y && node.Bounds.Bottom >= e.Y).
                        FirstOrDefault();
            }

            treeView.SelectedNode = selection;
            btnRemove.Enabled = selection != null;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            var win = new Form();

            var label = new Label {Text = "Node text (\f for format code):", AutoSize = true, Dock = DockStyle.Top};            
            var button = new Button {Text = "Add", Dock = DockStyle.Right};
            var input = new TextBox {Dock = DockStyle.Bottom};

            button.Click += (o, args) =>
                                {
                                    var selection = treeView.SelectedNode;
                                    var addition = Regex.Replace(input.Text, @"(?<!\\)\\f", "\f");
                                    if(selection == null)
                                    {
                                        treeView.Nodes.Add(addition);
                                    }
                                    else
                                    {
                                        selection.Nodes.Add(addition);
                                    }
                                    win.Close();
                                };
                        
            win.Controls.Add(input);
            win.Controls.Add(button);
            win.Controls.Add(label);
            win.AcceptButton = button;            

            win.Width = 300;
            win.Height = 72;

            win.Text = "Add Node";
            win.ShowDialog();
            input.Select();
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if(treeView.SelectedNode != null)
            {
                treeView.SelectedNode.Remove();
            }
        }
    }
}
