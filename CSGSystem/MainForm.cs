using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;


namespace CSGSystem
{
    public partial class MainForm : Form
    {
        TCamera cam;
        TRender render;
        //TCSGEngine engine;
        TEditor editor;
        PropertyGrid propertyGrid1;

        CSGObjects.TCSGBoolOp detail;

        public MainForm()
        {
            InitializeComponent();

            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);

            // The initial constructor code goes here.
            propertyGrid1 = new PropertyGrid();
            propertyGrid1.CommandsVisibleIfAvailable = true;
            propertyGrid1.Dock = DockStyle.Fill;
            propertyGrid1.TabIndex = 1;
            propertyGrid1.Text = "Property Grid";

            this.splitContainer2.Panel2.Controls.Add(propertyGrid1);
        }
        
        private void MainForm_Load(object sender, EventArgs e)
        {
            cam = new TCamera();
            render = new TRender(this.splitContainer1.Panel2);
            editor = new TEditor(cam, render);

            //инициализируем несколько объектов
            detail = new CSGSystem.CSGObjects.TCSGBoolOp();
            CSGObjects.TCSGBox box = new CSGSystem.CSGObjects.TCSGBox(2, 2, 1);
            //CSGObjects.TCSGSphere sp = new CSGSystem.CSGObjects.TCSGSphere(1);
            //CSGObjects.TCSGCylinder sp = new CSGSystem.CSGObjects.TCSGCylinder(1,2,30);
            //sp.SetRotation(Matrix.RotationX(3.14f / 4.0f));
            CSGObjects.TCSGBox sp = new CSGSystem.CSGObjects.TCSGBox( 1, 4, 4);
            detail.AddOperand(box);
            detail.AddOperand(sp);
            CSGObjects.TCSGSphere sp1 = new CSGSystem.CSGObjects.TCSGSphere(1);
            //sp1.SubDivLevel=0;
            detail.AddOperand(sp1);
            detail.Update();

            detail.BuildTreeView(treeView1.Nodes.Add("Деталь"));

            editor.Select(sp);
            propertyGrid1.SelectedObject = sp;

            timer1.Enabled = true;
            toolStripComboBox1.SelectedIndex = 0;
            toolStripTextBox1.Text = cam.GetDist().ToString();
            toolStripButton3.Checked = true;
            treeView1.ExpandAll();
        }
        private void splitContainer1_Panel2_MouseDown(object sender, MouseEventArgs e)
        {
            button1.Focus();
            if (e.Button == MouseButtons.Middle)
            {
                cam.StartRotating(e.Location);
            }
            else if (e.Button == MouseButtons.Right)
            {
                cam.StartPanning(e.Location);
            }
            else if (e.Button == MouseButtons.Left)
            {
                editor.OnMouseDown(e.Location);
            }
        }
        private void splitContainer1_Panel2_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                cam.EndRotating();
            }
            else if (e.Button == MouseButtons.Right)
            {
                cam.EndPanning();
            }
            else if (e.Button == MouseButtons.Left)
            {
                editor.OnMouseUp(e.Location);
            }
        }
        private void splitContainer1_Panel2_MouseMove(object sender, MouseEventArgs e)
        {
            cam.OnMouseMove(e.Location);
            editor.OnMouseMove(e.Location);
            toolStripStatusLabel1.Text = string.Format("Колесо мыши: поворот и масштаб; Правая кнопка: смещение");
        }
        private void MainForm_MouseWheel(object sender, MouseEventArgs e)
        {
            cam.OnMouseWheel(e.Delta / 120.0f);
            toolStripTextBox1.Text = cam.GetDist().ToString();
        }
        void DrawBackground()
        {
            render.Transform.Ortho();
            render.State.DepthTest(false);

            CustomVertex.PositionColored[] backgr = new CustomVertex.PositionColored[6];

            backgr[0].Color = System.Drawing.Color.FromArgb(1, 0, 0, 0).ToArgb();
            backgr[1].Color = backgr[0].Color;
            backgr[2].Color = System.Drawing.Color.FromArgb(1, 200, 200, 150).ToArgb();
            backgr[3].Color = backgr[2].Color;
            backgr[4].Color = backgr[2].Color;
            backgr[5].Color = backgr[0].Color;

            backgr[0].Position = new Vector3(-1, -1, 0);
            backgr[1].Position = new Vector3(1, -1, 1);
            backgr[2].Position = new Vector3(1, 1, 1);
            backgr[3].Position = new Vector3(1, 1, 0);
            backgr[4].Position = new Vector3(-1, 1, 0);
            backgr[5].Position = new Vector3(-1, -1, 0);

            render.Draw.Triangles(backgr);

            render.State.DepthTest(true);
            render.Transform.Projection();
            render.Transform.ViewByCam(cam);
        }
        private void Draw()
        {
            if (detail.Update())
                propertyGrid1.Refresh();
            render.BeginScene();
            //
            DrawBackground();

            render.State.CullFace(true);
            render.Lighting.Enable(true);
            render.Lighting.LightDir(cam.GetDir());
            //render.Lighting.Material(Color.Tomato, Color.Turquoise, 10);

            TTriMesh scene = detail.GetTransformedMesh();// engine.GetScene();
            CustomVertex.PositionNormal[] render_data = scene.GetRenderData();
            render.Draw.Triangles(render_data);

            if (this.toolStripButton2.Checked)
            {
                render.Lighting.Material(Color.Black, Color.Black, 0);
                render.State.DepthBias(true);
                render.State.FillModeWireFrame();
                render.Draw.Triangles(render_data);
                render.State.FillModeSolid();
                render.State.DepthBias(false);
            }
            render.Lighting.Enable(false);
            render.State.CullFace(false);

            render.ClearDepth();
            editor.DrawAxis(this.toolStripComboBox1.SelectedIndex==1,toolStripButton1.Checked);

            //TRay cursor_world = render.FromScreenToWorld(Cursor.Position);

            render.Transform.Ortho();
            render.State.DepthTest(false);
            render.Draw.Text(String.Format("Tri count: {0}",render_data.Length/3), new System.Drawing.Point(10, 30));
            ////Geometry.TAABB test_box = new Geometry.TAABB(new Vector3(0, 0, 0), new Vector3(0.5f, 0.5f, 0.5f));

            ////if (test_box.Overlaps(cursor_world))
            ////    render.Draw.Text("inters", new System.Drawing.Point(10, 30));

            //CustomVertex.PositionColored[] points = new CustomVertex.PositionColored[1];

            //points[0].Position = cursor_world.pos + cursor_world.dir;
            //points[0].Color = System.Drawing.Color.FromArgb(1, 0, 0, 0).ToArgb();

            render.Transform.Projection();
            render.Transform.ViewByCam(cam);
            //render.State.PointSize(10);
            //render.Draw.Points(points);

            render.State.DepthTest(true);

            render.EndScene();
        }
        //private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        //{
        //    Draw();
        //}
        private void timer1_Tick(object sender, EventArgs e)
        {
            Draw();
        }
        private void splitContainer1_Panel2_Click(object sender, EventArgs e)
        {
            if (this.button1.CanFocus)
            {
                this.button1.Focus();
            }
        }
        private void treeView1_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (((TreeNode)e.Item).Parent != null)
                DoDragDrop(e.Item, DragDropEffects.Move);
        }
        private void treeView1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }
        bool IsChildOf(TreeNode parent, TreeNode child)
        {
            if (child.Parent == parent) return true;
            else if (child.Parent != null) return IsChildOf(parent, child.Parent);
            else return false;
        }
        private void treeView1_DragDrop(object sender, DragEventArgs e)
        {
            TreeNode SourceNode;

            if (e.Data.GetDataPresent("System.Windows.Forms.TreeNode", false))
            {
                Point pt = ((TreeView)sender).PointToClient(new Point(e.X, e.Y));
                TreeNode DestinationNode = ((TreeView)sender).GetNodeAt(pt);
                SourceNode = (TreeNode)e.Data.GetData("System.Windows.Forms.TreeNode");
                if (DestinationNode == null || SourceNode == DestinationNode ||
                    IsChildOf(SourceNode, DestinationNode)||
                    SourceNode.Parent==null) return;

                if (propertyGrid1.SelectedObject == SourceNode.Tag)
                {
                    propertyGrid1.SelectedObject = null;
                    editor.Unselect();
                }
                if (((TCSGObject)(DestinationNode.Tag)) is CSGObjects.TCSGBoolOp)
                {
                    ((CSGObjects.TCSGBoolOp)(SourceNode.Parent.Tag)).RemoveOperand((TCSGObject)(SourceNode.Tag));
                    SourceNode.Parent.Nodes.Remove(SourceNode);
                    DestinationNode.Nodes.Add(SourceNode);
                    ((CSGObjects.TCSGBoolOp)(SourceNode.Parent.Tag)).AddOperand((TCSGObject)(SourceNode.Tag));
                    DestinationNode.Expand();
                }
                else
                {
                    ((CSGObjects.TCSGBoolOp)(SourceNode.Parent.Tag)).RemoveOperand((TCSGObject)(SourceNode.Tag));
                    TreeNode dest_parent = DestinationNode.Parent;
                    SourceNode.Parent.Nodes.Remove(SourceNode);
                    dest_parent.Nodes.Insert(dest_parent.Nodes.IndexOf(DestinationNode),SourceNode);
                    ((CSGObjects.TCSGBoolOp)(SourceNode.Parent.Tag)).InsertOperand((TCSGObject)(DestinationNode.Tag), (TCSGObject)(SourceNode.Tag));
                }
            }
        }
        private void toolStripTextBox1_TextChanged(object sender, EventArgs e)
        {
            string s=((ToolStripTextBox)sender).Text;
            if(s!="")
                cam.SetDist(float.Parse(s));
        }
        private void treeView1_MouseDown(object sender, MouseEventArgs e)
        {
            TreeNode node = ((TreeView)sender).GetNodeAt(e.Location);
            if (node != null)
            {
                treeView1.SelectedNode = node;
                editor.Select((TCSGObject)node.Tag);
                propertyGrid1.SelectedObject = node.Tag;
            }
        }
        void AddObjToTree(TreeNode parent_node, TCSGObject obj)
        {
            if (parent_node != null)
            {
                TCSGObject parent_obj = (TCSGObject)parent_node.Tag;
                if (parent_obj is CSGObjects.TCSGBoolOp)
                {
                    ((CSGObjects.TCSGBoolOp)parent_obj).AddOperand(obj);
                    TreeNode child_node = parent_node.Nodes.Add("");
                    obj.BuildTreeView(child_node);
                    treeView1.SelectedNode = child_node;
                    propertyGrid1.SelectedObject = obj;
                    editor.Select(obj);
                }
                else
                {
                    //TODO
                }
            }
        }
        private void operationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TCSGObject sub_obj = new CSGObjects.TCSGBoolOp();
            AddObjToTree(treeView1.SelectedNode, sub_obj);
        }
        private void boxToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TCSGObject sub_obj = new CSGObjects.TCSGBox(1,1,1);
            AddObjToTree(treeView1.SelectedNode, sub_obj);
        }

        private void sphereToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TCSGObject sub_obj = new CSGObjects.TCSGSphere(1);
            AddObjToTree(treeView1.SelectedNode, sub_obj);
        }

        private void cylinderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TCSGObject sub_obj = new CSGObjects.TCSGCylinder(1,2,30);
            AddObjToTree(treeView1.SelectedNode, sub_obj);
        }

        private void DeleteOperationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode node = treeView1.SelectedNode;
            if (node != null && node.Parent != null)
            {
                if (propertyGrid1.SelectedObject == node.Tag)
                {
                    propertyGrid1.SelectedObject = null;
                    editor.Unselect();
                }
                ((CSGObjects.TCSGBoolOp)(node.Parent.Tag)).RemoveOperand((TCSGObject)(node.Tag));
                node.Remove();

            }
        }
        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (treeView1.SelectedNode != null && treeView1.SelectedNode.Parent == null)
            {
                DeleteOperationToolStripMenuItem.Enabled = false;
            }
            else DeleteOperationToolStripMenuItem.Enabled = true;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            toolStripButton3.Checked = false;
            toolStripButton1.Checked = true;
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            toolStripButton3.Checked = true;
            toolStripButton1.Checked = false;
        }
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editor.Unselect();
            propertyGrid1.SelectedObject = null;
            detail.RemoveAllOperands();
            treeView1.Nodes.Clear();
            detail.BuildTreeView(treeView1.Nodes.Add("Деталь"));
        }

        private void openFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            editor.Unselect();
            propertyGrid1.SelectedObject = null;
            detail.RemoveAllOperands();
            treeView1.Nodes.Clear();
            //
            System.IO.Stream stream = openFileDialog1.OpenFile();
            System.IO.BinaryReader r = new System.IO.BinaryReader(stream);
            TCSGObject.TCSGObjectType det_typ=(TCSGObject.TCSGObjectType)r.ReadUInt32();
            if (det_typ != TCSGObject.TCSGObjectType.BOOL_OP) MessageBox.Show("");
            detail.Serialize(stream,true);
            stream.Close();
            detail.Update();
            //
            detail.BuildTreeView(treeView1.Nodes.Add("Деталь"));
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog(this);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.ShowDialog(this);
        }

        private void saveFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //
            System.IO.Stream stream = saveFileDialog1.OpenFile();
            detail.Serialize(stream, false);
            //
        }

        private void treeView1_MouseMove(object sender, MouseEventArgs e)
        {
            toolStripStatusLabel1.Text = "Правый щелчок для изменения объектов";
        }
    }
}
