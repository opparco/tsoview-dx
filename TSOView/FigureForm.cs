using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
//using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using TDCG;

namespace TSOView
{
    /// <summary>
    /// フィギュア情報を扱うフォーム
    /// </summary>
public partial class FigureForm : Form
{
    /// <summary>
    /// フィギュア情報フォームを生成します。
    /// </summary>
    public FigureForm()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Escを押すと抜けます。
    /// </summary>
    /// <param name="e">イベント引数</param>
    protected override void OnKeyPress(System.Windows.Forms.KeyPressEventArgs e)
    {
        if ((int)(byte)e.KeyChar == (int)System.Windows.Forms.Keys.Escape)
            this.Dispose(); // Esc was pressed
    }

    private Figure fig = null;
    private TSOFile tso = null;
    private TSOSubScript sub_script = null;

    /// <summary>
    /// フィギュア情報を削除します。
    /// </summary>
    public void Clear()
    {
        gvShaderParams.DataSource = null;
        this.sub_script = null;
        lvSubScripts.Items.Clear();
        this.tso = null;
        lvTSOFiles.Items.Clear();
        this.fig = null;
    }

    /// <summary>
    /// フィギュアをUIに設定します。
    /// </summary>
    /// <param name="fig">フィギュア</param>
    public void SetFigure(Figure fig)
    {
        this.fig = fig;

        this.tbSlideArm.Value = (int)(fig.slider_matrix.ArmRate * (float)tbSlideArm.Maximum);
        this.tbSlideLeg.Value = (int)(fig.slider_matrix.LegRate * (float)tbSlideLeg.Maximum);
        this.tbSlideWaist.Value = (int)(fig.slider_matrix.WaistRate * (float)tbSlideWaist.Maximum);
        this.tbSlideOppai.Value = (int)(fig.slider_matrix.OppaiRate * (float)tbSlideOppai.Maximum);
        this.tbSlideAge.Value = (int)(fig.slider_matrix.AgeRate * (float)tbSlideAge.Maximum);
        this.tbSlideEye.Value = (int)(fig.slider_matrix.EyeRate * (float)tbSlideEye.Maximum);

        lvTSOFiles.Items.Clear();
        for (int i = 0; i < fig.TSOFileList.Count; i++)
        {
            TSOFile tso = fig.TSOFileList[i];
            ListViewItem li = new ListViewItem(tso.FileName ?? "TSO #" + i.ToString());
            li.Tag = tso;
            lvTSOFiles.Items.Add(li);
        }
        lvTSOFiles.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
    }

    /// <summary>
    /// tsoをUIに設定します。
    /// </summary>
    /// <param name="tso">tso</param>
    public void SetTSOFile(TSOFile tso)
    {
        this.tso = tso;
        lvSubScripts.Items.Clear();
        foreach (TSOSubScript sub_script in tso.sub_scripts)
        {
            ListViewItem li = new ListViewItem(sub_script.Name);
            li.SubItems.Add(sub_script.FileName);
            li.Tag = sub_script;
            lvSubScripts.Items.Add(li);
        }
        lvSubScripts.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
    }

    public void SetSubScript(TSOSubScript sub_script)
    {
        this.sub_script = sub_script;
        gvShaderParams.DataSource = sub_script.shader.shader_parameters;
    }

    private void btnDump_Click(object sender, EventArgs e)
    {
        if (sub_script == null)
            return;
        Console.WriteLine("-- dump shader parameters --");
        foreach (string str in sub_script.shader.GetLines())
            Console.WriteLine(str);
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        if (tso == null)
            return;
        if (sub_script != null)
            sub_script.SaveShader();

        SaveFileDialog dialog = new SaveFileDialog();
        dialog.FileName = tso.FileName;
        dialog.Filter = "tso files|*.tso";
        dialog.FilterIndex = 0;
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            string dest_file = dialog.FileName;
            string extension = Path.GetExtension(dest_file);
            if (extension == ".tso")
            {
                tso.Save(dest_file);
            }
        }
    }

    private void btnUp_Click(object sender, EventArgs e)
    {
        if (lvTSOFiles.SelectedItems.Count == 0)
            return;
        int li_idx = lvTSOFiles.SelectedIndices[0];
        int li_idx_prev = li_idx - 1;
        if (li_idx_prev < 0)
            return;
        fig.SwapAt(li_idx_prev, li_idx);
        SetFigure(fig);
        ListViewItem li = lvTSOFiles.Items[li_idx_prev];
        li.Selected = true;
    }

    private void btnDown_Click(object sender, EventArgs e)
    {
        if (lvTSOFiles.SelectedItems.Count == 0)
            return;
        int li_idx = lvTSOFiles.SelectedIndices[0];
        int li_idx_next = li_idx + 1;
        if (li_idx_next > lvTSOFiles.Items.Count - 1)
            return;
        fig.SwapAt(li_idx, li_idx_next);
        SetFigure(fig);
        ListViewItem li = lvTSOFiles.Items[li_idx_next];
        li.Selected = true;
    }

    private void lvTSOFiles_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (lvTSOFiles.SelectedItems.Count == 0)
            return;
        ListViewItem li = lvTSOFiles.SelectedItems[0];
        TSOFile tso = li.Tag as TSOFile;
        SetTSOFile(tso);
    }

    private void lvSubScripts_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (lvSubScripts.SelectedItems.Count == 0)
            return;
        ListViewItem li = lvSubScripts.SelectedItems[0];
        TSOSubScript sub_script = li.Tag as TSOSubScript;
        SetSubScript(sub_script);
    }

    private void FigureForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.FormOwnerClosing)
        {
            this.Hide();
            e.Cancel = true;
        }
    }

    private void tbSlideArm_ValueChanged(object sender, EventArgs e)
    {
        if (fig == null)
            return;

        fig.slider_matrix.ArmRate = tbSlideArm.Value / (float)tbSlideArm.Maximum;
        fig.UpdateBoneMatrices(true);
    }

    private void tbSlideLeg_ValueChanged(object sender, EventArgs e)
    {
        if (fig == null)
            return;

        fig.slider_matrix.LegRate = tbSlideLeg.Value / (float)tbSlideLeg.Maximum;
        fig.UpdateBoneMatrices(true);
    }

    private void tbSlideWaist_ValueChanged(object sender, EventArgs e)
    {
        if (fig == null)
            return;

        fig.slider_matrix.WaistRate = tbSlideWaist.Value / (float)tbSlideWaist.Maximum;
        fig.UpdateBoneMatrices(true);
    }

    private void tbSlideOppai_ValueChanged(object sender, EventArgs e)
    {
        if (fig == null)
            return;

        fig.slider_matrix.OppaiRate = tbSlideOppai.Value / (float)tbSlideOppai.Maximum;
        fig.UpdateBoneMatrices(true);
    }

    private void tbSlideAge_ValueChanged(object sender, EventArgs e)
    {
        if (fig == null)
            return;

        fig.slider_matrix.AgeRate = tbSlideAge.Value / (float)tbSlideAge.Maximum;
        fig.UpdateBoneMatrices(true);
    }

    private void tbSlideEye_ValueChanged(object sender, EventArgs e)
    {
        if (fig == null)
            return;

        fig.slider_matrix.EyeRate = tbSlideEye.Value / (float)tbSlideEye.Maximum;
        fig.UpdateBoneMatrices(true);
    }
}
}
