<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        components = New ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Form1))
        Label1 = New Label()
        TextChars = New TextBox()
        ButtonGenTable = New Button()
        Label2 = New Label()
        PanelAuto = New Panel()
        LabelFile = New Label()
        TextFileName = New TextBox()
        ButtonExport = New Button()
        CheckH = New CheckBox()
        CheckV = New CheckBox()
        LabelStatus = New Label()
        PanelPreview = New Panel()
        GlyphCanvas1 = New GlyphCanvas()
        PanelPreview.SuspendLayout()
        SuspendLayout()
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Location = New Point(12, 12)
        Label1.Name = "Label1"
        Label1.Size = New Size(116, 17)
        Label1.TabIndex = 0
        Label1.Text = "第一步：输入字符集"
        ' 
        ' TextChars
        ' 
        TextChars.AcceptsReturn = True
        TextChars.Font = New Font("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        TextChars.Location = New Point(12, 34)
        TextChars.Multiline = True
        TextChars.Name = "TextChars"
        TextChars.ScrollBars = ScrollBars.Vertical
        TextChars.Size = New Size(300, 180)
        TextChars.TabIndex = 1
        TextChars.Text = "01234" & vbCrLf & "56789" & vbCrLf & "ABCDE"
        TextChars.WordWrap = False
        ' 
        ' ButtonGenTable
        ' 
        ButtonGenTable.Location = New Point(12, 222)
        ButtonGenTable.Name = "ButtonGenTable"
        ButtonGenTable.Size = New Size(300, 34)
        ButtonGenTable.TabIndex = 2
        ButtonGenTable.Text = "第二步：生成图案表"
        ButtonGenTable.UseVisualStyleBackColor = True
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Location = New Point(12, 268)
        Label2.Name = "Label2"
        Label2.Size = New Size(130, 17)
        Label2.TabIndex = 3
        Label2.Text = "第三步：拖放PNG图片"
        ' 
        ' PanelAuto
        ' 
        PanelAuto.BackColor = Color.FromArgb(CByte(247), CByte(247), CByte(247))
        PanelAuto.BorderStyle = BorderStyle.FixedSingle
        PanelAuto.Location = New Point(12, 290)
        PanelAuto.Name = "PanelAuto"
        PanelAuto.Size = New Size(300, 86)
        PanelAuto.TabIndex = 4
        ' 
        ' LabelFile
        ' 
        LabelFile.AutoSize = True
        LabelFile.Location = New Point(12, 388)
        LabelFile.Name = "LabelFile"
        LabelFile.Size = New Size(128, 17)
        LabelFile.TabIndex = 5
        LabelFile.Text = "第四步：导出字体文件"
        ' 
        ' TextFileName
        ' 
        TextFileName.Font = New Font("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        TextFileName.Location = New Point(12, 408)
        TextFileName.Name = "TextFileName"
        TextFileName.Size = New Size(140, 26)
        TextFileName.TabIndex = 6
        TextFileName.Text = "Font_"
        ' 
        ' ButtonExport
        ' 
        ButtonExport.Location = New Point(160, 406)
        ButtonExport.Name = "ButtonExport"
        ButtonExport.Size = New Size(152, 32)
        ButtonExport.TabIndex = 7
        ButtonExport.Text = "生成并导出"
        ButtonExport.UseVisualStyleBackColor = True
        ' 
        ' CheckH
        ' 
        CheckH.AutoSize = True
        CheckH.Location = New Point(12, 448)
        CheckH.Name = "CheckH"
        CheckH.Size = New Size(75, 21)
        CheckH.TabIndex = 8
        CheckH.Text = "水平居中"
        CheckH.UseVisualStyleBackColor = True
        ' 
        ' CheckV
        ' 
        CheckV.AutoSize = True
        CheckV.Location = New Point(12, 474)
        CheckV.Name = "CheckV"
        CheckV.Size = New Size(75, 21)
        CheckV.TabIndex = 9
        CheckV.Text = "垂直居中"
        CheckV.UseVisualStyleBackColor = True
        ' 
        ' LabelStatus
        ' 
        LabelStatus.ForeColor = Color.FromArgb(CByte(90), CByte(90), CByte(90))
        LabelStatus.Location = New Point(12, 506)
        LabelStatus.Name = "LabelStatus"
        LabelStatus.Size = New Size(300, 130)
        LabelStatus.TabIndex = 10
        LabelStatus.Text = "① 输入字符 → ② 生成图案表 → ③ 拖放PNG → ④ 导出字体"
        ' 
        ' PanelPreview
        ' 
        PanelPreview.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        PanelPreview.AutoScroll = True
        PanelPreview.BackColor = Color.FromArgb(CByte(240), CByte(240), CByte(240))
        PanelPreview.BorderStyle = BorderStyle.FixedSingle
        PanelPreview.Controls.Add(GlyphCanvas1)
        PanelPreview.Location = New Point(324, 12)
        PanelPreview.Name = "PanelPreview"
        PanelPreview.Size = New Size(664, 676)
        PanelPreview.TabIndex = 11
        ' 
        ' GlyphCanvas1
        ' 
        GlyphCanvas1.AllowDrop = True
        GlyphCanvas1.BackColor = Color.White
        GlyphCanvas1.CellSize = 56
        GlyphCanvas1.Cols = 0
        GlyphCanvas1.Location = New Point(0, 0)
        GlyphCanvas1.Name = "GlyphCanvas1"
        GlyphCanvas1.Rows = 0
        GlyphCanvas1.Size = New Size(662, 674)
        GlyphCanvas1.TabIndex = 0
        GlyphCanvas1.TabStop = False
        ' 
        ' Form1
        ' 
        AutoScaleDimensions = New SizeF(7F, 17F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1000, 700)
        Controls.Add(PanelPreview)
        Controls.Add(LabelStatus)
        Controls.Add(CheckV)
        Controls.Add(CheckH)
        Controls.Add(ButtonExport)
        Controls.Add(TextFileName)
        Controls.Add(LabelFile)
        Controls.Add(PanelAuto)
        Controls.Add(Label2)
        Controls.Add(ButtonGenTable)
        Controls.Add(TextChars)
        Controls.Add(Label1)
        MinimumSize = New Size(820, 560)
        Name = "Form1"
        StartPosition = FormStartPosition.CenterScreen
        Text = "位图字体生成 v0.7"
        PanelPreview.ResumeLayout(False)
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents Label1 As Label
    Friend WithEvents TextChars As TextBox
    Friend WithEvents ButtonGenTable As Button
    Friend WithEvents Label2 As Label
    Friend WithEvents PanelAuto As Panel
    Friend WithEvents LabelFile As Label
    Friend WithEvents TextFileName As TextBox
    Friend WithEvents ButtonExport As Button
    Friend WithEvents CheckH As CheckBox
    Friend WithEvents CheckV As CheckBox
    Friend WithEvents LabelStatus As Label
    Friend WithEvents PanelPreview As Panel
    Friend WithEvents GlyphCanvas1 As GlyphCanvas

End Class
