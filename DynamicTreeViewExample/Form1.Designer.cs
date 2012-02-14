namespace DynamicTreeViewExample
{
    partial class Form1
    {
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.btnAdd = new System.Windows.Forms.ToolStripMenuItem();
            this.btnRemove = new System.Windows.Forms.ToolStripMenuItem();
            this.treeView = new DynamicTreeView.DynamicTreeView();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnAdd,
            this.btnRemove});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(153, 70);
            // 
            // btnAdd
            // 
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(152, 22);
            this.btnAdd.Text = "Add";
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // btnRemove
            // 
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(152, 22);
            this.btnRemove.Text = "Remove";
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // treeView
            // 
            this.treeView.ArrowRenderStyle = DynamicTreeView.DynamicTreeViewRenderStyle.Native;
            this.treeView.AutoScroll = true;
            this.treeView.AutoScrollMinSize = new System.Drawing.Size(0, 4);
            this.treeView.ContextMenuStrip = this.contextMenuStrip1;
            this.treeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView.HighlightArrow = null;
            this.treeView.HighlightNode = null;
            this.treeView.Location = new System.Drawing.Point(0, 0);
            this.treeView.Name = "treeView";
            this.treeView.NodeSpacing = new System.Drawing.Size(4, 4);
            this.treeView.Root = null;
            this.treeView.SelectedNode = null;
            this.treeView.SelectionRenderStyle = DynamicTreeView.DynamicTreeViewRenderStyle.Native;
            this.treeView.Size = new System.Drawing.Size(447, 316);
            this.treeView.TabIndex = 0;
            this.treeView.Text = "dynamicTreeView1";
            this.treeView.NodeMouseDown += new DynamicTreeView.DynamicTreeNodeMouseEventHandler(this.treeView_NodeMouseDown);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(447, 316);
            this.Controls.Add(this.treeView);
            this.Name = "Form1";
            this.Text = "Dynamic Tree View Example";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private DynamicTreeView.DynamicTreeView treeView;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem btnAdd;
        private System.Windows.Forms.ToolStripMenuItem btnRemove;
    }
}

