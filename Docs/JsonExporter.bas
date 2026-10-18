Attribute VB_Name = "JsonExporter"
Option Explicit

' 범용 JSON 내보내기
' - 버튼을 누른 시트(ActiveSheet)를 JSON 으로 만든다.
' - 1행 헤더를 A열부터 빈 칸이 나올 때까지 키로 사용한다. (열을 추가해도 코드 수정 불필요)
' - 결과는 이 엑셀 파일 옆 Json\<폴더>\<파일>.json 에 바로 저장된다.
' - 새 시트를 추가하려면 GetJsonTarget 에 Case 한 줄만 추가하면 된다.

Private Function GetJsonTarget(ByVal sheetName As String) As String
    ' 반환값: "폴더\파일명.json"  (빈 문자열이면 미등록 시트)
    Select Case sheetName
        Case "인벤토리 아이템": GetJsonTarget = "InvenItem\InvenItem.json"
        Case "스킬": GetJsonTarget = "Skill\Skill.json"
        Case Else: GetJsonTarget = ""
    End Select
End Function

Public Sub ExportSheetJson()

    Dim ws As Worksheet
    Set ws = ActiveSheet

    Dim target As String
    target = GetJsonTarget(ws.Name)
    If target = "" Then
        MsgBox "'" & ws.Name & "' 시트는 JsonExporter.GetJsonTarget 에 등록되어 있지 않습니다.", vbExclamation
        Exit Sub
    End If

    ' 헤더: A1 부터 빈 칸 전까지
    Dim lastCol As Long
    lastCol = 0
    Do While Trim(CStr(ws.Cells(1, lastCol + 1).Value)) <> ""
        lastCol = lastCol + 1
    Loop
    If lastCol = 0 Then
        MsgBox "1행에 헤더가 없습니다.", vbExclamation
        Exit Sub
    End If

    Dim lastRow As Long
    lastRow = ws.Cells(ws.Rows.Count, 1).End(xlUp).Row

    Dim json As String
    Dim r As Long, c As Long, count As Long
    Dim key As String
    Dim value As Variant

    json = "[" & vbCrLf
    count = 0

    For r = 2 To lastRow
        If Trim(CStr(ws.Cells(r, 1).Value)) <> "" Then

            If count > 0 Then json = json & "," & vbCrLf
            json = json & "  {" & vbCrLf

            For c = 1 To lastCol
                key = Trim(CStr(ws.Cells(1, c).Value))
                value = ws.Cells(r, c).Value

                json = json & "    """ & key & """:" & ToJsonValue(value)
                If c < lastCol Then json = json & ","
                json = json & vbCrLf
            Next c

            json = json & "  }"
            count = count + 1
        End If
    Next r

    json = json & vbCrLf & "]"

    ' 저장 위치 준비
    Dim filePath As String
    filePath = ThisWorkbook.Path & "\Json\" & target
    EnsureFolder Left(filePath, InStrRev(filePath, "\") - 1)

    Dim stream As Object
    Set stream = CreateObject("ADODB.Stream")
    stream.Type = 2
    stream.Charset = "utf-8"
    stream.Open
    stream.WriteText json
    stream.SaveToFile filePath, 2
    stream.Close

    MsgBox count & "행 → JSON 생성 완료!" & vbCrLf & filePath

End Sub

Private Function ToJsonValue(ByVal value As Variant) As String
    Dim s As String
    If IsError(value) Then
        ToJsonValue = """"""
        Exit Function
    End If
    s = Trim(CStr(value))
    If s = "" Then
        ToJsonValue = """"""
    ElseIf VarType(value) = vbBoolean Then
        ToJsonValue = IIf(value, "1", "0")
    ElseIf IsNumeric(value) And VarType(value) <> vbString Then
        ToJsonValue = Replace(CStr(value), ",", ".")
    Else
        s = Replace(CStr(value), "\", "\\")
        s = Replace(s, """", "\""")
        s = Replace(s, vbCrLf, "\n")
        s = Replace(s, vbLf, "\n")
        s = Replace(s, vbCr, "\n")
        s = Replace(s, vbTab, "\t")
        ToJsonValue = """" & s & """"
    End If
End Function

Private Sub EnsureFolder(ByVal folderPath As String)
    Dim fso As Object
    Set fso = CreateObject("Scripting.FileSystemObject")
    If Not fso.FolderExists(folderPath) Then
        EnsureFolder fso.GetParentFolderName(folderPath)
        fso.CreateFolder folderPath
    End If
End Sub
