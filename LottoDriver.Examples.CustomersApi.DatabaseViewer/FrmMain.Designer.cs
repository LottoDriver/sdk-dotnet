namespace LottoDriver.Examples.CustomersApi.DatabaseViewer
{
    partial class FrmMain
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
            this._dataGrid = new System.Windows.Forms.DataGridView();
            this.bindingSource1 = new System.Windows.Forms.BindingSource(this.components);
            this.dataSet1 = new System.Data.DataSet();
            this.dataTable1 = new System.Data.DataTable();
            this.dataColumn1 = new System.Data.DataColumn();
            this.dataColumn2 = new System.Data.DataColumn();
            this.dataColumn3 = new System.Data.DataColumn();
            this.dataColumn4 = new System.Data.DataColumn();
            this.dataColumn5 = new System.Data.DataColumn();
            this.dataColumn6 = new System.Data.DataColumn();
            this.dataColumn7 = new System.Data.DataColumn();
            this.dataColumn8 = new System.Data.DataColumn();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.dataColumn9 = new System.Data.DataColumn();
            this.idDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lottodriver_draw_id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.scheduledtimeutcDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.drawtimeutcDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.recommended_closing_time_utc = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lottonameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.status = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.resultDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.extra_result = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this._dataGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataSet1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataTable1)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // _dataGrid
            // 
            this._dataGrid.AllowUserToAddRows = false;
            this._dataGrid.AllowUserToDeleteRows = false;
            this._dataGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._dataGrid.AutoGenerateColumns = false;
            this._dataGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this._dataGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this._dataGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.idDataGridViewTextBoxColumn,
            this.lottodriver_draw_id,
            this.scheduledtimeutcDataGridViewTextBoxColumn,
            this.drawtimeutcDataGridViewTextBoxColumn,
            this.recommended_closing_time_utc,
            this.lottonameDataGridViewTextBoxColumn,
            this.status,
            this.resultDataGridViewTextBoxColumn,
            this.extra_result});
            this._dataGrid.DataSource = this.bindingSource1;
            this._dataGrid.Location = new System.Drawing.Point(4, 47);
            this._dataGrid.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this._dataGrid.Name = "_dataGrid";
            this._dataGrid.ReadOnly = true;
            this._dataGrid.RowHeadersWidth = 51;
            this._dataGrid.Size = new System.Drawing.Size(1412, 707);
            this._dataGrid.TabIndex = 0;
            // 
            // bindingSource1
            // 
            this.bindingSource1.DataMember = "LottoDraw";
            this.bindingSource1.DataSource = this.dataSet1;
            // 
            // dataSet1
            // 
            this.dataSet1.DataSetName = "LottoDraw";
            this.dataSet1.Tables.AddRange(new System.Data.DataTable[] {
            this.dataTable1});
            // 
            // dataTable1
            // 
            this.dataTable1.Columns.AddRange(new System.Data.DataColumn[] {
            this.dataColumn1,
            this.dataColumn2,
            this.dataColumn3,
            this.dataColumn4,
            this.dataColumn5,
            this.dataColumn6,
            this.dataColumn7,
            this.dataColumn8,
            this.dataColumn9});
            this.dataTable1.TableName = "LottoDraw";
            // 
            // dataColumn1
            // 
            this.dataColumn1.Caption = "Id";
            this.dataColumn1.ColumnName = "id";
            this.dataColumn1.DataType = typeof(int);
            // 
            // dataColumn2
            // 
            this.dataColumn2.Caption = "Scheduled Time UTC";
            this.dataColumn2.ColumnName = "scheduled_time_utc";
            this.dataColumn2.DataType = typeof(System.DateTime);
            // 
            // dataColumn3
            // 
            this.dataColumn3.Caption = "Draw Time UTC";
            this.dataColumn3.ColumnName = "draw_time_utc";
            this.dataColumn3.DataType = typeof(System.DateTime);
            // 
            // dataColumn4
            // 
            this.dataColumn4.Caption = "Lotto Name";
            this.dataColumn4.ColumnName = "lotto_name";
            // 
            // dataColumn5
            // 
            this.dataColumn5.Caption = "Status";
            this.dataColumn5.ColumnName = "status";
            this.dataColumn5.DataType = typeof(int);
            // 
            // dataColumn6
            // 
            this.dataColumn6.Caption = "Result";
            this.dataColumn6.ColumnName = "result";
            // 
            // dataColumn7
            // 
            this.dataColumn7.Caption = "LottoDriver ID";
            this.dataColumn7.ColumnName = "lottodriver_draw_id";
            this.dataColumn7.DataType = typeof(long);
            // 
            // dataColumn8
            // 
            this.dataColumn8.ColumnName = "recommended_closing_time_utc";
            this.dataColumn8.DataType = typeof(System.DateTime);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this._dataGrid, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.btnRefresh, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 43F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1420, 758);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // btnRefresh
            // 
            this.btnRefresh.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.btnRefresh.Location = new System.Drawing.Point(4, 7);
            this.btnRefresh.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(100, 28);
            this.btnRefresh.TabIndex = 1;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // dataColumn9
            // 
            this.dataColumn9.Caption = "Extra Result";
            this.dataColumn9.ColumnName = "extra_result";
            // 
            // idDataGridViewTextBoxColumn
            // 
            this.idDataGridViewTextBoxColumn.DataPropertyName = "id";
            this.idDataGridViewTextBoxColumn.HeaderText = "Id";
            this.idDataGridViewTextBoxColumn.MinimumWidth = 6;
            this.idDataGridViewTextBoxColumn.Name = "idDataGridViewTextBoxColumn";
            this.idDataGridViewTextBoxColumn.ReadOnly = true;
            this.idDataGridViewTextBoxColumn.Width = 47;
            // 
            // lottodriver_draw_id
            // 
            this.lottodriver_draw_id.DataPropertyName = "lottodriver_draw_id";
            this.lottodriver_draw_id.HeaderText = "LottoDriver Id";
            this.lottodriver_draw_id.MinimumWidth = 6;
            this.lottodriver_draw_id.Name = "lottodriver_draw_id";
            this.lottodriver_draw_id.ReadOnly = true;
            this.lottodriver_draw_id.Width = 115;
            // 
            // scheduledtimeutcDataGridViewTextBoxColumn
            // 
            this.scheduledtimeutcDataGridViewTextBoxColumn.DataPropertyName = "scheduled_time_utc";
            this.scheduledtimeutcDataGridViewTextBoxColumn.HeaderText = "Scheduled Time UTC";
            this.scheduledtimeutcDataGridViewTextBoxColumn.MinimumWidth = 6;
            this.scheduledtimeutcDataGridViewTextBoxColumn.Name = "scheduledtimeutcDataGridViewTextBoxColumn";
            this.scheduledtimeutcDataGridViewTextBoxColumn.ReadOnly = true;
            this.scheduledtimeutcDataGridViewTextBoxColumn.Width = 127;
            // 
            // drawtimeutcDataGridViewTextBoxColumn
            // 
            this.drawtimeutcDataGridViewTextBoxColumn.DataPropertyName = "draw_time_utc";
            this.drawtimeutcDataGridViewTextBoxColumn.HeaderText = "Draw Time UTC";
            this.drawtimeutcDataGridViewTextBoxColumn.MinimumWidth = 6;
            this.drawtimeutcDataGridViewTextBoxColumn.Name = "drawtimeutcDataGridViewTextBoxColumn";
            this.drawtimeutcDataGridViewTextBoxColumn.ReadOnly = true;
            this.drawtimeutcDataGridViewTextBoxColumn.Width = 121;
            // 
            // recommended_closing_time_utc
            // 
            this.recommended_closing_time_utc.DataPropertyName = "recommended_closing_time_utc";
            this.recommended_closing_time_utc.HeaderText = "Closing Time UTC";
            this.recommended_closing_time_utc.MinimumWidth = 6;
            this.recommended_closing_time_utc.Name = "recommended_closing_time_utc";
            this.recommended_closing_time_utc.ReadOnly = true;
            this.recommended_closing_time_utc.Width = 109;
            // 
            // lottonameDataGridViewTextBoxColumn
            // 
            this.lottonameDataGridViewTextBoxColumn.DataPropertyName = "lotto_name";
            this.lottonameDataGridViewTextBoxColumn.HeaderText = "Lotto Name";
            this.lottonameDataGridViewTextBoxColumn.MinimumWidth = 6;
            this.lottonameDataGridViewTextBoxColumn.Name = "lottonameDataGridViewTextBoxColumn";
            this.lottonameDataGridViewTextBoxColumn.ReadOnly = true;
            this.lottonameDataGridViewTextBoxColumn.Width = 97;
            // 
            // status
            // 
            this.status.DataPropertyName = "status";
            this.status.HeaderText = "Status";
            this.status.MinimumWidth = 6;
            this.status.Name = "status";
            this.status.ReadOnly = true;
            this.status.Width = 73;
            // 
            // resultDataGridViewTextBoxColumn
            // 
            this.resultDataGridViewTextBoxColumn.DataPropertyName = "result";
            this.resultDataGridViewTextBoxColumn.HeaderText = "Result";
            this.resultDataGridViewTextBoxColumn.MinimumWidth = 6;
            this.resultDataGridViewTextBoxColumn.Name = "resultDataGridViewTextBoxColumn";
            this.resultDataGridViewTextBoxColumn.ReadOnly = true;
            this.resultDataGridViewTextBoxColumn.Width = 74;
            // 
            // extra_result
            // 
            this.extra_result.DataPropertyName = "extra_result";
            this.extra_result.HeaderText = "Extra Result";
            this.extra_result.MinimumWidth = 6;
            this.extra_result.Name = "extra_result";
            this.extra_result.ReadOnly = true;
            this.extra_result.Width = 99;
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1420, 758);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "FrmMain";
            this.Text = "Customers API - Database Viewer";
            ((System.ComponentModel.ISupportInitialize)(this._dataGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataSet1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataTable1)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView _dataGrid;
        private System.Data.DataSet dataSet1;
        private System.Data.DataTable dataTable1;
        private System.Data.DataColumn dataColumn1;
        private System.Data.DataColumn dataColumn2;
        private System.Data.DataColumn dataColumn3;
        private System.Data.DataColumn dataColumn4;
        private System.Data.DataColumn dataColumn5;
        private System.Data.DataColumn dataColumn6;
        private System.Windows.Forms.BindingSource bindingSource1;
        private System.Data.DataColumn dataColumn7;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button btnRefresh;
        private System.Data.DataColumn dataColumn8;
        private System.Windows.Forms.DataGridViewTextBoxColumn idDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn lottodriver_draw_id;
        private System.Windows.Forms.DataGridViewTextBoxColumn scheduledtimeutcDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn drawtimeutcDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn recommended_closing_time_utc;
        private System.Windows.Forms.DataGridViewTextBoxColumn lottonameDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn status;
        private System.Windows.Forms.DataGridViewTextBoxColumn resultDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn extra_result;
        private System.Data.DataColumn dataColumn9;
    }
}

