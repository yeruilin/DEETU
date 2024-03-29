﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Sunny.UI;
using DEETU.Core;
using DEETU.Map;
using DEETU.IO;
using DEETU.Tool;
using DEETU.Geometry;

namespace DEETU.Source.Window
{
    /// <summary>
    /// 属性表界面
    /// </summary>
    public partial class AttributeTableForm : UIForm
    {
        #region 字段
        private GeoMapLayer mLayer;
        bool mIsEditing = false;

        #endregion
        public AttributeTableForm(GeoMapLayer layer, bool isEditing)
        {
            InitializeComponent();
            // 同步图层和和编辑状态
            mLayer = layer;
            mIsEditing = isEditing;
            
            // 两个Page的初始化
            InitializeFormPage();
            InitializeGridPage();

            // 设置和编辑状态相关的工具条显示
            SetEdit();

            // 设置切换tab的UI
            Header.SelectedIndex = 0;
            Header.SetNodeSymbol(Header.Nodes[0], 61451);
            Header.SetNodeSymbol(Header.Nodes[1], 61450);
        }

        #region 事件处理函数
        private void Header_MenuItemClick(string itemText, int menuIndex, int pageIndex)
        {
            uiTabControl1.SelectedIndex = menuIndex;
        }

        private void featureList_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Clear all
            mLayer.SelectedFeatures.Clear();
            foreach (ListViewItem item in featureList.Items)
                item.ImageIndex = 0;
            
            if (!featureList.SelectedItems.IsNullOrEmpty())
            {
                //只显示第一个被选中的要素
                GeoFeature feature = featureList.SelectedItems[0].Tag as GeoFeature;
                ShowFeatureOnDetailTable(feature);
                foreach (ListViewItem item in featureList.SelectedItems)
                {
                    item.ImageIndex = 1;
                    mLayer.SelectedFeatures.Add(item.Tag as GeoFeature);
                }
            }
            MapRedraw?.Invoke(this);
        }

        private void reloadToolStripButton_Click
            (object sender, EventArgs e)
        {
            ReloadPages();
        }


        private void featureDataGridView_SelectionChanged(object sender, EventArgs e)
        {
            // Clear all
            mLayer.SelectedFeatures.Clear();

            foreach (DataGridViewRow row in featureDataGridView.SelectedRows)
            {
                mLayer.SelectedFeatures.Add(row.Tag as GeoFeature);
            }
            
            MapRedraw?.Invoke(this);
        }

        private void editToolStripButton_Click(object sender, EventArgs e)
        {
            mIsEditing = !mIsEditing;
            MapEditStatusChanged?.Invoke(this, mIsEditing);
            SetEdit();
        }

        private void addFeatureToolStripButton_Click(object sender, EventArgs e)
        {
            FeatureAdded?.Invoke(this, mLayer);
        }

        private void removeFeatureToolStripButton_Click(object sender, EventArgs e)
        {
            if (mLayer.SelectedFeatures.Count == 0)
            {
                UIMessageBox.ShowError("请选择要素", false);
                return;
            }
            FeatureRemoved?.Invoke(this, mLayer);

        }

        private void addFieldStripButton_Click(object sender, EventArgs e)
        {
            UIEditOption option = new UIEditOption();
            option.AutoLabelWidth = true;
            option.Text = "增加一个字段";
            option.AddText("Name", "字段名称", "", true);
            option.AddText("AliasName", "别名", "", false);
            option.AddCombobox("Type", "数据类型", Enum.GetNames(typeof(GeoValueTypeConstant)));

            UIEditForm editForm = new UIEditForm(option);
            editForm.ShowDialog();

            if (editForm.IsOK)
            {
                GeoField newField = new GeoField((string)editForm["Name"], (GeoValueTypeConstant)editForm["Type"]);
                newField.AliaName = (string)editForm["AliasName"];
                mLayer.AttributeFields.Append(newField);
                ReloadPages();
            }
        }

        private void removeFieldToolStripButton_Click(object sender, EventArgs e)
        {
            GeoFields fields = mLayer.AttributeFields;
            fields.RemoveAt(fields.Count - 1);
        }

        private void selectByExpressionToolStripButton_Click(object sender, EventArgs e)
        {

            SelectedByExpressionForm expressionForm = new SelectedByExpressionForm(mLayer);
            expressionForm.LayerQuery += ExpressionForm_LayerQuery; // 处理按表达式选择的事件
            expressionForm.ShowDialog();
        }

        private void ExpressionForm_LayerQuery(object sender, GeoMapLayer layer, string expression, GeoSelectionModeConstant selectionMode)
        {
            LayerQuery?.Invoke(this, mLayer, expression, selectionMode); // 把这个按表达式查询的时间传递给主窗口
            ReloadPages();           
        }

        private void selectAllToolStripButton_Click(object sender, EventArgs e)
        {
            if (mLayer.Features.Count == 0)
                return;
            // ban selectedChanged
            featureList.SelectedIndexChanged -= featureList_SelectedIndexChanged;
            for (int i = 1; i < featureList.Items.Count; i++)
            {
                featureList.Items[i].Selected = true;
            }
            // recover selectedChanged
            featureList.SelectedIndexChanged += featureList_SelectedIndexChanged;
            // 以上这些操作是为了防止界面反复刷新出现卡顿
            featureList.Items[0].Selected = true;
            MapRedraw?.Invoke(this);
            InitializeGridPage();
        }

        private void removeSelectToolStripButton_Click(object sender, EventArgs e)
        {
            if (mLayer.Features.Count == 0)
                return;
            // ban selectedChanged
            featureList.SelectedIndexChanged -= featureList_SelectedIndexChanged;
            for (int i = 1; i < featureList.Items.Count; i++)
            {
                featureList.Items[i].Selected = false;
            }
            // recover selectedChanged
            featureList.SelectedIndexChanged += featureList_SelectedIndexChanged;
            // 以上这些操作是为了防止界面反复刷新出现卡顿
            featureList.Items[0].Selected = false;
            featureList_SelectedIndexChanged(sender, e);
            MapRedraw?.Invoke(this);
            InitializeGridPage();
        }

        private void cutToolStripButton_Click(object sender, EventArgs e)
        {
            FeatureCut?.Invoke(this, mLayer);
        }

        private void copyStripButton_Click(object sender, EventArgs e)
        {
            FeatureCopied?.Invoke(this, mLayer);
        }

        private void pasteToolStripButton_Click(object sender, EventArgs e)
        {
            FeaturePasted?.Invoke(this, mLayer);
        }

        /// <summary>
        /// 在编辑结束后保存编辑到图层
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void featureDataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (featureDataGridView.SelectedCells.IsNullOrEmpty())
                return;
            DataGridViewCell cell = featureDataGridView.SelectedCells[0];
            GeoFeature feature = mLayer.Features.GetItem(cell.RowIndex);
            GeoField field = mLayer.AttributeFields.GetItem(cell.ColumnIndex);
            try
            {
                switch (field.ValueType)
                {
                    case GeoValueTypeConstant.dInt16:
                        feature.Attributes.SetItem(cell.ColumnIndex, Convert.ToInt16(cell.Value));
                        break;
                    case GeoValueTypeConstant.dInt32:
                        feature.Attributes.SetItem(cell.ColumnIndex, Convert.ToInt32(cell.Value));
                        break;
                    case GeoValueTypeConstant.dInt64:
                        feature.Attributes.SetItem(cell.ColumnIndex, Convert.ToInt64(cell.Value));
                        break;
                    case GeoValueTypeConstant.dSingle:
                        feature.Attributes.SetItem(cell.ColumnIndex, Convert.ToSingle(cell.Value));
                        break;
                    case GeoValueTypeConstant.dDouble:
                        feature.Attributes.SetItem(cell.ColumnIndex, Convert.ToDouble(cell.Value));
                        break;
                    case GeoValueTypeConstant.dText:
                        feature.Attributes.SetItem(cell.ColumnIndex, Convert.ToString(cell.Value));
                        break;
                    default:
                        throw new Exception();
                        break;
                }
                InitializeGridPage();
            }
            catch (Exception error)
            {
                UIMessageBox.ShowError("错误的数据类型！\n" + error.Message, false);
            }
        }

        /// <summary>
        /// 在编辑结束后保存编辑到图层
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            UITextBox textBox = sender as UITextBox;
            int featureIdx = featureList.SelectedItems[0].Index;
            int fieldIdx = detailTable.GetRow(textBox);
            GeoFeature feature = mLayer.Features.GetItem(featureIdx);
            GeoField field = mLayer.AttributeFields.GetItem(fieldIdx);
            if (textBox.Text.IsNullOrWhiteSpace())
                return;
            try
            {
                switch (field.ValueType)
                {
                    case GeoValueTypeConstant.dInt16:
                        feature.Attributes.SetItem(fieldIdx, Convert.ToInt16(textBox.Text));
                        break;
                    case GeoValueTypeConstant.dInt32:
                        feature.Attributes.SetItem(fieldIdx, Convert.ToInt32(textBox.Text));
                        break;
                    case GeoValueTypeConstant.dInt64:
                        feature.Attributes.SetItem(fieldIdx, Convert.ToInt64(textBox.Text));
                        break;
                    case GeoValueTypeConstant.dSingle:
                        feature.Attributes.SetItem(fieldIdx, Convert.ToSingle(textBox.Text));
                        break;
                    case GeoValueTypeConstant.dDouble:
                        feature.Attributes.SetItem(fieldIdx, Convert.ToDouble(textBox.Text));
                        break;
                    case GeoValueTypeConstant.dText:
                        feature.Attributes.SetItem(fieldIdx, (textBox.Text));
                        break;
                    default:
                        throw new Exception();
                        break;
                }
            }
            catch (Exception error)
            {
                UIMessageBox.ShowError("错误的数据类型！\n" + error.Message, false);
            }
        }

        /// <summary>
        /// 在切换界面的时候重新读取一次, 避免出现不一致
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void uiTabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (uiTabControl1.SelectedIndex == 0)
                InitializeFormPage();
            else
                InitializeGridPage();
        }

        /// <summary>
        /// 在编辑结束后保存编辑到图层
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void featureDataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (featureDataGridView.SelectedCells.IsNullOrEmpty())
                return;
            DataGridViewCell cell = featureDataGridView.SelectedCells[0];
            GeoFeature feature = mLayer.Features.GetItem(cell.RowIndex);
            GeoField field = mLayer.AttributeFields.GetItem(cell.ColumnIndex);
            if (cell.Value.ToString().IsNullOrEmpty())
                return;
            try
            {
                switch (field.ValueType)
                {
                    case GeoValueTypeConstant.dInt16:
                        feature.Attributes.SetItem(cell.ColumnIndex, Convert.ToInt16(cell.Value));
                        break;
                    case GeoValueTypeConstant.dInt32:
                        feature.Attributes.SetItem(cell.ColumnIndex, Convert.ToInt32(cell.Value));
                        break;
                    case GeoValueTypeConstant.dInt64:
                        feature.Attributes.SetItem(cell.ColumnIndex, Convert.ToInt64(cell.Value));
                        break;
                    case GeoValueTypeConstant.dSingle:
                        feature.Attributes.SetItem(cell.ColumnIndex, Convert.ToSingle(cell.Value));
                        break;
                    case GeoValueTypeConstant.dDouble:
                        feature.Attributes.SetItem(cell.ColumnIndex, Convert.ToDouble(cell.Value));
                        break;
                    case GeoValueTypeConstant.dText:
                        feature.Attributes.SetItem(cell.ColumnIndex, Convert.ToString(cell.Value));
                        break;
                    default:
                        throw new Exception();
                        break;
                }
                InitializeGridPage();
            }
            catch (Exception error)
            {
                UIMessageBox.ShowError("错误的数据类型！\n" + error.Message, false);
            }
        }

        #endregion

        #region 私有函数
        /// <summary>
        /// FormPage 初始化
        /// </summary>
        private void InitializeFormPage()
        {
            // 先禁用选择改变事件 
            featureList.SelectedIndexChanged -= featureList_SelectedIndexChanged;

            detailTable.Controls.Clear();
            featureList.Clear();

            // Detailed panel
            GeoFields fields = mLayer.AttributeFields;
            detailTable.RowCount = fields.Count;

            for (int i = 0; i < fields.Count; i++)
            {
                // label
                UILabel label = new UILabel();
                label.Text = fields.GetItem(i).AliaName;
                label.Font = new System.Drawing.Font("微软雅黑", 10f);
                label.Dock = DockStyle.Top;

                UITextBox textBox = new UITextBox();
                textBox.Font = new System.Drawing.Font("微软雅黑", 10f);
                textBox.ReadOnly = false;
                textBox.Dock = DockStyle.Top;
                textBox.TextChanged += TextBox_TextChanged;

                detailTable.Controls.Add(label, 0, i);
                detailTable.Controls.Add(textBox, 1, i);

            }

            // List View
            GeoFeatures features = mLayer.Features;
            GeoFeatures selectedFeatures = mLayer.SelectedFeatures;
            for (int i = 0; i < features.Count; i++)
            {
                ListViewItem item = new ListViewItem(features.GetItem(i).Attributes.GetItem(0).ToString());
                if (item.Text.IsNullOrEmpty())
                {
                    item.Text = "Untitled" + item.Index.ToString();
                }
                item.Tag = features.GetItem(i);
                item.ImageIndex = 0;

                for (int j = 0; j < selectedFeatures.Count; j++)
                {
                    if (selectedFeatures.GetItem(j) == features.GetItem(i))
                    {
                        item.ImageIndex = 1;
                        item.Selected = true;
                        
                        break;
                    }
                }
                featureList.Items.Add(item);
            }

            if (featureList.SelectedItems.Count > 0)
            {
                GeoFeature feature = featureList.SelectedItems[0].Tag as GeoFeature;
                ShowFeatureOnDetailTable(feature);
            }
            
            // 复用选择改变事件 
            featureList.SelectedIndexChanged += featureList_SelectedIndexChanged;

        }

        /// <summary>
        /// GridPage 初始化
        /// </summary>
        private void InitializeGridPage()
        {
            // 禁用DataGridViewChanged
            featureDataGridView.SelectionChanged -= featureDataGridView_SelectionChanged;

            featureDataGridView.ClearAll();
            // Columns
            GeoFields fields = mLayer.AttributeFields;
            for (int i = 0; i < fields.Count; i++)
            {
                featureDataGridView.AddColumn(fields.GetItem(i).AliaName, null);
                featureDataGridView.Columns[i].DefaultCellStyle.Font = (new Font("微软雅黑", 10f));
                featureDataGridView.Columns[i].SortMode = DataGridViewColumnSortMode.Automatic;
            }

            GeoFeatures features = mLayer.Features;
            GeoFeatures selectedFeatures = mLayer.SelectedFeatures;
            for (int i = 0; i < features.Count; i++)
            {
                GeoFeature feature = features.GetItem(i);
                object[] rowValue = new object[fields.Count];
                for (int j = 0; j < feature.Attributes.Count; j++)
                {
                    rowValue[j] = feature.Attributes.GetItem(j);
                }
                featureDataGridView.AddRow(rowValue);
                featureDataGridView.Rows[i].Tag = feature;
                

                for (int j = 0; j < selectedFeatures.Count; j++)
                {
                    if (selectedFeatures.GetItem(j) == features.GetItem(i))
                    {
                        featureDataGridView.Rows[i].Selected = true;
                        break;
                    }
                }
            }

            // 复用DataGridViewChanged
            featureDataGridView.SelectionChanged += featureDataGridView_SelectionChanged;

            // !解决第一次点edit之后featureDataGridView无法编辑的问题，可能是控件本身的bug，用了一个非常sb的解决方式
            featureDataGridView.ReadOnly = !featureDataGridView.ReadOnly;
            featureDataGridView.ReadOnly = !featureDataGridView.ReadOnly;

        }

        /// <summary>
        /// 刷新界面
        /// </summary>
        private void ReloadPages()
        {
            InitializeFormPage();
            InitializeGridPage();
        }

        /// <summary>
        /// 动态显示当前选择的Feature
        /// </summary>
        /// <param name="feature"> 在FormPage上选中的Feature</param>
        private void ShowFeatureOnDetailTable(GeoFeature feature)
        {
            for (int i = 0; i < feature.Attributes.Count; i++)
            {
                UITextBox textBox = detailTable.GetControlFromPosition(1, i) as UITextBox;
                textBox.Text = feature.Attributes.GetItem(i).ToString();
            }
        }

        /// <summary>
        /// 设置与编辑状态相关的可见性
        /// </summary>
        private void SetEdit()
        {
            startEditToolStripButton.Checked = mIsEditing;
            if (mIsEditing)
            {
                startEditToolStripButton.Image = new Bitmap("./icons/edit_off.png");
                startEditToolStripButton.ToolTipText = "结束编辑";
                featureDataGridView.SelectionMode = DataGridViewSelectionMode.RowHeaderSelect;
            }
            else
            {
                startEditToolStripButton.Image = new Bitmap("./icons/edit.png");
                startEditToolStripButton.ToolTipText = "开始编辑";
                featureDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            }

            cutToolStripButton.Enabled = mIsEditing;
            pasteToolStripButton.Enabled = mIsEditing;
            copyStripButton.Enabled = mIsEditing;
            addFeatureToolStripButton.Enabled = mIsEditing;
            removeFeatureToolStripButton.Enabled = mIsEditing;
            addFieldStripButton.Enabled = mIsEditing;
            removeFieldToolStripButton.Enabled = mIsEditing;

            featureDataGridView.ReadOnly = !mIsEditing;
            for (int i = 0; i < detailTable.RowCount; i++)
            {
                UITextBox textBox = detailTable.GetControlFromPosition(1, i) as UITextBox;
                textBox.ReadOnly = !mIsEditing;
            }
        }
        #endregion

        #region 事件
        public delegate void MapRedrawHandle(object sender);
        /// <summary>
        /// 使主界面中的MapControl redraw
        /// </summary>
        /// <param name="sender"></param>
        public event MapRedrawHandle MapRedraw;

        public delegate void MapEditStatusChangedHandle(object sender, bool status);
        /// <summary>
        /// Set MainPage MapControl Start editding;
        /// </summary>
        public event MapEditStatusChangedHandle MapEditStatusChanged;

        public delegate void LayerQueryHandler(object sender, GeoMapLayer layer, string expression, GeoSelectionModeConstant selectionMode);
        /// <summary>
        /// 传递按表达式查询的时间
        /// </summary>
        public event LayerQueryHandler LayerQuery;

        /// <summary>
        /// 传递一系列编辑操作到主窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="layer"></param>
        public delegate void FeatureEditHandle(object sender, GeoMapLayer layer);
        public event FeatureEditHandle FeatureCopied;
        public event FeatureEditHandle FeatureCut;
        public event FeatureEditHandle FeaturePasted;
        public event FeatureEditHandle FeatureAdded;
        public event FeatureEditHandle FeatureRemoved;
        #endregion

        /// <summary>
        /// 接受主窗口的活动图层变更事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="layer"></param>
        public void MainPage_CurrentActiveLayerChanged(object sender, GeoMapLayer layer)
        {
            mLayer = layer;
            ReloadPages();
        }

        /// <summary>
        /// 接受主窗口的编辑状态变更，主要是为了同步
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="status"></param>
        public void MainPage_EditStatusChanged(object sender, bool status)
        {
            mIsEditing = status;
            SetEdit();
        }

    }
}
