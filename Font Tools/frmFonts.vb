Imports johnpaulsmith.Common.Data

Public Class frmFonts


    Dim LIST_SAMPLE_SIZE As Single = 18

    Dim sDSFilename As String
    Dim DS As New DataSet

    Private bRedrawInProgress As Boolean = False


    Private Sub frmFonts_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing

        JohnPaulSmith.Common.WinForms.FormHelper.SaveSettings(Me, My.Settings)

        SaveDatabase()

    End Sub

    Private Sub UpdateStatus(ByVal Message As String)
        ToolStripStatusLabel1.Text = Message
        Application.DoEvents()
    End Sub
    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        Me.Cursor = Cursors.WaitCursor

        JohnPaulSmith.Common.WinForms.FormHelper.LoadSettings(Me, My.Settings)

        UpdateStatus("Loading Database...")

        'load the data set
        sDSFilename = My.Computer.FileSystem.SpecialDirectories.MyDocuments
        sDSFilename = System.IO.Path.Combine(sDSFilename, "Font Preview.ds")
        If System.IO.File.Exists(sDSFilename) Then
            DS.Load(sDSFilename)
        Else
            CreateDatabase()
        End If

       initlistview()


        'Try
        '    Lobo.Common.Controls.ListViewHelper.SetGroupCollapseAll(lvwFonts, Lobo.Common.Controls.ListViewHelper.GroupState.COLLAPSIBLE)
        '    Lobo.Common.Controls.ListViewHelper.SetGroupCollapse(lvwFonts, "_Hidden", Lobo.Common.Controls.ListViewHelper.GroupState.COLLAPSED)

        'Catch ex As Exception
        '    Debug.Print("oops")
        'End Try

        Try
            SaveDatabase()
        Catch ex As Exception
            Debug.Assert(False)
        End Try

        Try
            ShowFontNames()
        Catch ex As Exception
            Debug.Assert(False)
        End Try

        lvwFonts.EndUpdate()

        UpdateStatus("Ready")

    End Sub

    Private Sub InitListView()


        UpdateStatus("Initialising Listview...")

        Me.lvwFonts.BeginUpdate()

        CreateGroup("_Hidden")
        CreateGroup("Dings")
        CreateGroup("Script")
        CreateGroup("Serif")
        CreateGroup("San-Serif")
        CreateGroup("Gothic")
        CreateGroup("Decorative")

        UpdateStatus("Initialising Listview... Done")

        UpdateStatus("Creating Groups...")
        Me.lvwFonts.BeginUpdate()

        For Each aRow As DataRow In DS.Tables("FontGroups").Rows

            Dim sGroupName As String = ""
            Try
                sGroupName = aRow.Item("GroupName").ToString.Trim
            Catch ex As Exception

            End Try
            If sGroupName <> "" Then
                CreateGroup(sGroupName)
            End If

        Next
        Me.lvwFonts.EndUpdate()

        UpdateStatus("Creating Groups... Done")

    End Sub
    Private Function CreateGroup(ByVal GroupName As String) As ListViewGroup

        Dim ThisGroup As ListViewGroup = JohnPaulSmith.Common.WinForms.ListViewHelper.GetGroup(lvwFonts, GroupName)

        'create a menu item for the group
        Dim mnuItem As New ToolStripMenuItem(GroupName, Nothing)
                mnuGroups.Items.Add(mnuItem)
                mnuItem.Tag = ThisGroup
                AddHandler mnuItem.Click, AddressOf mnuGroupItem_Click
   
        Return ThisGroup

    End Function

    Private Sub CreateDatabase()

        DS = New JohnPaulSmith.Common.Data.DataSet
        Dim DT As DataTable = DS.CreateTable("FontGroups")
        DT.CreateColumn("ID", DataTable.eDataType.Integer, True)
        DT.CreateColumn("GroupName")
        DT.CreateColumn("FontName")
        DT.CreateColumn("State", DataTable.eDataType.Boolean)

        DS.AcceptChanges()

    End Sub

    Private Sub SaveDatabase()

        UpdateStatus("Saving Database...")

        DS.Save(sDSFilename)

        UpdateStatus("Saving Database... Done")

    End Sub

    Private Function GetListViewItem(ByVal Key As String, ByVal Name As String) As ListViewItem

        Dim ThisItem As ListViewItem = Nothing

        'first see if the item already exists
        ThisItem = lvwFonts.Items(Key)

        If ThisItem Is Nothing Then
            ThisItem = lvwFonts.Items.Add(Key, Name, 0)

            Dim aRow As DataRow = Nothing
            Dim bFound As Boolean = False
            For Each MyRow As DataRow In DS.Tables(0).Rows
                If MyRow.Item("FontName") = Name Then
                    bFound = True
                    aRow = MyRow
                    Exit For
                End If
            Next

            If bFound = False Then
                aRow = DS.Tables(0).NewRow()
                aRow.Item("FontName") = Name
                aRow.Item("State") = True
                DS.Tables(0).Rows.Add(aRow)
            End If

            ThisItem.Tag = aRow
            ThisItem.Checked = True
            'SaveDatabase()
        End If


        Return ThisItem

    End Function

    Private Sub ShowFontNames()

        Me.Cursor = Cursors.WaitCursor
        ToolStripStatusLabel1.Text = "Showing Fonts..."
        Dim iCount As Integer = 0

        Application.DoEvents()
        bRedrawInProgress = True

        lvwFonts.BeginUpdate()

        'enumerate the installed fonts
        Dim Fonts As New System.Drawing.Text.InstalledFontCollection
        Dim FontFamilies() As System.Drawing.FontFamily = Fonts.Families

        For Each Font As FontFamily In FontFamilies

            Dim sKey As String = Font.Name.Replace(" ", "_").Replace("#", "")
            Dim anItem As ListViewItem = GetListViewItem(sKey, Font.Name)
            Dim FontRow As DataRow = anItem.Tag

            Dim ThisGroup As ListViewGroup = Nothing
            Dim sGroupName As String = ""
            Try
                sGroupName = FontRow.Item("GroupName")
            Catch ex As Exception

            End Try

            ThisGroup = CreateGroup(sGroupName)
            If Not ThisGroup Is Nothing Then
                ThisGroup.Items.Add(anItem)
            End If

            anItem.Checked = FontRow.Item("State")
            anItem.UseItemStyleForSubItems = False

            anItem.SubItems.Add(New ListViewItem.ListViewSubItem())
            Try
                ' anItem.SubItems(1).Font = New Font(Font.Name, LIST_SAMPLE_SIZE)
                anItem.SubItems(1).Text = "This is a sample"
            Catch ex As Exception
                anItem.SubItems(1).Text = ex.Message
            End Try

            iCount += 1
            ToolStripStatusLabel1.Text = "Showing Fonts... " & iCount.ToString & " of " & FontFamilies.Count.ToString
            Application.DoEvents()

        Next

        bRedrawInProgress = False

        Me.Cursor = Cursors.Default
        lvwFonts.EndUpdate()
        UpdateStatus("Showing Fonts... Done")

    End Sub

    Private Sub DrawFonts()

        Me.Cursor = Cursors.WaitCursor
        Application.DoEvents()
        bRedrawInProgress = True

        Dim iFontSize As Integer = 12

        If Me.cmbCmbSize.Text <> "" Then
            iFontSize = Me.cmbCmbSize.Text
        End If

        RichTextBox1.Clear()

        'first draw the fonts that are not in a group
        For Each anItem As ListViewItem In lvwFonts.Items

            If anItem.Group Is Nothing Then
                DrawFont(anItem, iFontSize)
            End If
        Next

        For Each aGroup As ListViewGroup In lvwFonts.Groups
            If aGroup.Name <> "_Hidden" Then
                Dim iCount As Integer = 0
                For Each anItem As ListViewItem In aGroup.Items
                    If anItem.Checked Then
                        iCount += 1
                    End If
                Next
                If iCount > 0 Then
                    DrawGroup(aGroup.Name)
                    For Each anItem As ListViewItem In aGroup.Items
                        DrawFont(anItem, iFontSize)
                    Next
                End If

            End If
        Next

        bRedrawInProgress = False
        Me.Cursor = Cursors.Default
        Application.DoEvents()

    End Sub
    Private Sub DrawGroup(ByVal GroupName As String)
        With RichTextBox1
            .Select(.TextLength, 0)
            Try
                .SelectionFont = New Font("Microsoft Sans Serif", 22)
            Catch ex As Exception

            End Try

            .SelectionColor = Color.Orange

            .AppendText("Group: " & GroupName & vbCrLf)

        End With
    End Sub
    Private Sub DrawFont(ByVal anItem As ListViewItem, ByVal iFontSize As Integer)

        If anItem.Checked Then
            Dim sSample As String = anItem.Text '& vbCrLf
            If Me.txtSample.Text <> "" Then
                sSample = Me.txtSample.Text
            End If

            With RichTextBox1
                .Select(.TextLength, 0)
                Try
                    .SelectionFont = New Font(anItem.Text, iFontSize)
                Catch ex As Exception

                End Try

                .AppendText(sSample)

                'If Me.txtSample.Text <> "" Then
                .Select(.TextLength, 0)
                .SelectionFont = New Font("Microsoft Sans Serif", 8.2)
                .AppendText(" [" & anItem.Text & "]" & vbCrLf)
                'End If

            End With

        End If 'if anitem.checked

        Application.DoEvents()

    End Sub
    Private Sub cmbCmbSize_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmbCmbSize.SelectedIndexChanged
        If cmbCmbSize.SelectedIndex > 0 Then
            DrawFonts()
        End If
    End Sub

    Private Sub btnRefresh_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnRefresh.Click
        DrawFonts()
    End Sub
    Private Sub mnuGroupItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

        Dim mnu As ToolStripMenuItem = sender

        For Each ListItem As ListViewItem In lvwFonts.SelectedItems

            Dim Row As DataRow = ListItem.Tag

            Row.Item("GroupName") = mnu.Text

            ListItem.Group = CreateGroup(mnu.Text)
        Next

        SaveDatabase()
        DrawFonts()

    End Sub
    Private Sub lvwFonts_ItemChecked(ByVal sender As Object, ByVal e As System.Windows.Forms.ItemCheckedEventArgs) Handles lvwFonts.ItemChecked

        If bRedrawInProgress Then
            Exit Sub
        End If

        Dim Item As ListViewItem = e.Item
        Dim Row As DataRow = Item.Tag

        Row.Item("State") = Item.Checked
        DrawFonts()
        SaveDatabase()

    End Sub

    Private Sub btnUncheck_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnUncheck.Click
        bRedrawInProgress = True

        For Each anItem As ListViewItem In lvwFonts.Items
            anItem.Checked = False
            Dim Row As DataRow = anItem.Tag
            Row.Item("State") = anItem.Checked
        Next
        bRedrawInProgress = False
        DrawFonts()
        SaveDatabase()

    End Sub

    Private Sub CheckToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CheckToolStripMenuItem.Click

        bRedrawInProgress = True

        For Each anItem In lvwFonts.SelectedItems
            anItem.checked = True
        Next
        bRedrawInProgress = False
        DrawFonts()
        SaveDatabase()

    End Sub
End Class
