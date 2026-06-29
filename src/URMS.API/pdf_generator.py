"""
KICSIT Result PDF Generator  v2
Generates:
  1. Individual Student Result Card  (matches Image 1 — letter A4 portrait)
  2. Class Result Sheet              (matches Image 2 — landscape spreadsheet)

SGPA = Σ(GradePoints × CreditHours) / Σ(CreditHours)
CGPA = Σ(all semester SGPAs up to current) / N
Both stored to 9dp, rounded to 2dp for display.
Semester 1 → SGPA = CGPA (no previous semesters).
"""
from reportlab.lib.pagesizes import A4, landscape
from reportlab.lib import colors
from reportlab.lib.units import cm
from reportlab.lib.styles import ParagraphStyle
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, HRFlowable
)
from reportlab.lib.enums import TA_CENTER, TA_LEFT, TA_RIGHT
import os

BLACK  = colors.black
WHITE  = colors.white
GRAY   = colors.HexColor("#555555")
LGRAY  = colors.HexColor("#BBBBBB")
DGRAY  = colors.HexColor("#D0D0D0")

# ── Grade scale (KICSIT) ──────────────────────────────────────────────────────
GRADES = {
    "A":   4.00, "A-":  3.67,
    "B+":  3.33, "B":   3.00, "B-":  2.67,
    "C+":  2.33, "C":   2.00, "C-":  1.67,
    "D+":  1.33, "D":   1.00,
    "F":   0.00, "W/C": 0.00, "":    0.00,
}

def gp(grade): return GRADES.get(grade, 0.00)

def calc_sgpa(subjects):
    """subjects = [{grade, credit_hours}]"""
    wt = sum(gp(s["grade"]) * s["credit_hours"] for s in subjects)
    ch = sum(s["credit_hours"] for s in subjects if s["grade"] not in ("","F","W/C") or True)
    ch_eff = sum(s["credit_hours"] for s in subjects)
    return wt / ch_eff if ch_eff else 0.0

def calc_cgpa(sgpa_list): return sum(sgpa_list)/len(sgpa_list) if sgpa_list else 0.0

def academic_status(cgpa):
    if cgpa >= 3.50: return "Excellent"
    if cgpa >= 3.00: return "Very Good"
    if cgpa >= 2.50: return "Good"
    if cgpa >= 2.00: return "Satisfactory"
    if cgpa >= 1.50: return "Fair"
    if cgpa >= 1.00: return "Warning"
    return "Extended Temporary Enrollment"

def S(name, **kw): return ParagraphStyle(name, **kw)

# ═══════════════════════════════════════════════════════════════════════════════
# 1.  INDIVIDUAL STUDENT RESULT CARD
# ═══════════════════════════════════════════════════════════════════════════════
def generate_student_result_card(
    output_path,
    university_name = "Dr. A. Q. Khan Institute of Computer Sciences &\nInformation Technology",
    campus_line     = "Campus of Institute of Space Technology, Islamabad",
    address_line    = "KRL Kahuta, Distt. Rawalpindi, Pakistan.    Tel +92.51.9285059, Fax +92.51.9285245",
    website         = "www.kicsit.edu.pk",
    parent_name     = "Gul Nisar",
    student_name    = "Muhammad Saleh Hayat",
    reg_no          = "232201070",
    class_name      = "BCS-9",
    semester_label  = "4th Semester",
    result_title    = "Result – Spring – 2025",
    subjects        = None,   # [{code, name, credit_hours, grade}]
    previous_sgpas  = None,   # list of float (empty for semester 1)
    current_pos     = 14,
    overall_pos     = 23,
    total_students  = 83,
    examiner_title  = "Deputy Controller of Examinations",
):
    subjects       = subjects       or []
    previous_sgpas = previous_sgpas or []

    sgpa_raw = calc_sgpa(subjects)
    all_sgpas = previous_sgpas + [sgpa_raw]
    cgpa_raw  = calc_cgpa(all_sgpas)
    sgpa_2dp  = f"{round(sgpa_raw,2):.2f}"
    cgpa_2dp  = f"{round(cgpa_raw,2):.2f}"
    acad_stat = academic_status(cgpa_raw)

    # margin 2.5 cm each side → content width
    LM = RM = 2.5*cm
    CW = A4[0] - LM - RM    # ~453.5 pt

    doc = SimpleDocTemplate(output_path, pagesize=A4,
                            leftMargin=LM, rightMargin=RM,
                            topMargin=1.5*cm, bottomMargin=2.0*cm)

    # styles
    h_uni  = S("h_uni",  fontSize=13, fontName="Helvetica-Bold",   alignment=TA_CENTER, leading=16)
    h_cam  = S("h_cam",  fontSize=11, fontName="Helvetica-Bold",   alignment=TA_CENTER, leading=14)
    h_addr = S("h_addr", fontSize=8,  fontName="Helvetica",        alignment=TA_CENTER, textColor=GRAY)
    h_web  = S("h_web",  fontSize=8,  fontName="Helvetica",        alignment=TA_CENTER, textColor=colors.blue)
    body   = S("body",   fontSize=10, fontName="Helvetica",        leading=14, spaceAfter=3)
    b_bold = S("bbold",  fontSize=10, fontName="Helvetica-Bold",   leading=14)
    th_c   = S("th_c",   fontSize=9,  fontName="Helvetica-Bold",   alignment=TA_CENTER, leading=11)
    td_c   = S("td_c",   fontSize=9,  fontName="Helvetica",        alignment=TA_CENTER, leading=11)
    td_l   = S("td_l",   fontSize=9,  fontName="Helvetica",        alignment=TA_LEFT,   leading=11)
    gpa_l  = S("gpa_l",  fontSize=9,  fontName="Helvetica-Bold",   alignment=TA_RIGHT,  leading=11)
    gpa_v  = S("gpa_v",  fontSize=9,  fontName="Helvetica-Bold",   alignment=TA_CENTER, leading=11)
    gpa_o  = S("gpa_o",  fontSize=9,  fontName="Helvetica",        alignment=TA_LEFT,   leading=11)
    sig_c  = S("sig_c",  fontSize=9,  fontName="Helvetica",        alignment=TA_CENTER, leading=12)
    foot   = S("foot",   fontSize=8,  fontName="Helvetica-Oblique",alignment=TA_LEFT,   textColor=GRAY)

    story = []

    # ── HEADER (logo | text | logo) ──────────────────────────────────────────
    LOGO_W  = 1.8*cm
    TEXT_W  = CW - 2*LOGO_W

    # logo boxes
    def logo_box(label):
        t = Table([[label]], colWidths=[LOGO_W], rowHeights=[LOGO_W])
        t.setStyle(TableStyle([
            ("BOX",           (0,0),(0,0), 0.5, LGRAY),
            ("ALIGN",         (0,0),(0,0), "CENTER"),
            ("VALIGN",        (0,0),(0,0), "MIDDLE"),
            ("FONTSIZE",      (0,0),(0,0), 8),
            ("TEXTCOLOR",     (0,0),(0,0), LGRAY),
        ]))
        return t

    txt_rows = [
        [Paragraph(university_name.replace("\n","<br/>"), h_uni)],
        [Paragraph(campus_line,  h_cam)],
        [Paragraph(address_line, h_addr)],
        [Paragraph(f'<u>{website}</u>', h_web)],
    ]
    txt_tbl = Table(txt_rows, colWidths=[TEXT_W])
    txt_tbl.setStyle(TableStyle([("TOPPADDING",(0,0),(-1,-1),1),("BOTTOMPADDING",(0,0),(-1,-1),1)]))

    hdr = Table([[logo_box("ist"), txt_tbl, logo_box("🏛")]],
                colWidths=[LOGO_W, TEXT_W, LOGO_W])
    hdr.setStyle(TableStyle([
        ("VALIGN",(0,0),(-1,-1),"MIDDLE"),
        ("TOPPADDING",(0,0),(-1,-1),0),
        ("BOTTOMPADDING",(0,0),(-1,-1),0),
    ]))
    story.append(hdr)
    story.append(HRFlowable(width="100%", thickness=1, color=BLACK, spaceAfter=8))

    # ── TO / SUBJECT ────────────────────────────────────────────────────────
    story.append(Paragraph("To:", body))
    story.append(Spacer(1, 2))
    story.append(Paragraph(f"&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<b>{parent_name}</b>", body))
    story.append(Spacer(1, 8))
    story.append(Paragraph(f"Subject: &nbsp;&nbsp;&nbsp;&nbsp; <b><u>{result_title}</u></b>", body))
    story.append(Spacer(1, 10))
    story.append(Paragraph("Dear Parents/Guardians", body))
    story.append(Spacer(1, 6))
    story.append(Paragraph(
        f"1.&nbsp;&nbsp; Your son/daughter/ward <b><u>{student_name}</u></b> "
        f"Registration No. <b><u>{reg_no}</u></b> of Class <b><u>{class_name}</u></b> has "
        f"scored the following grades in the &nbsp;<u>{semester_label}</u>&nbsp; exams.", body))
    story.append(Spacer(1, 10))

    # ── COURSE TABLE ────────────────────────────────────────────────────────
    # 6 columns: No | Code | Name | CreditHr | Grade | (blank for GPA rows)
    # For data rows the last col is blank; for GPA rows cols 4&5 used for value+out
    # We use a unified 6-col table: No | Code | Name | CreditHr | Grade | extra
    # GPA rows span cols 0-2 blank, col3=label, col4=value, col5="Out of 4.00"
    
    W0 = 1.0*cm   # No
    W1 = 2.4*cm   # Code
    W3 = 1.8*cm   # Credit Hour
    W4 = 1.4*cm   # Grade
    W5 = 2.5*cm   # "Out of 4.00"
    W_LBL = 3.0*cm  # GPA label
    W2 = CW - W0 - W1 - W3 - W4 - W5  # Name — fills remainder
    col_w = [W0, W1, W2, W3, W4, W5]

    # header row
    rows = [[
        Paragraph("S.<br/>No.", th_c),
        Paragraph("Course<br/>Code", th_c),
        Paragraph("Course Name", th_c),
        Paragraph("Credit<br/>Hour", th_c),
        Paragraph("Grade", th_c),
        Paragraph("", th_c),
    ]]

    for i, s in enumerate(subjects, 1):
        rows.append([
            Paragraph(str(i), td_c),
            Paragraph(s["code"], td_c),
            Paragraph(s["name"], td_l),
            Paragraph(str(s["credit_hours"]), td_c),
            Paragraph(s["grade"], td_c),
            "",
        ])

    n_data = len(subjects)

    # SGPA row: cols 0-2 empty, col3=label, col4=value, col5=Out of
    rows.append([
        "", "", "",
        Paragraph("Semester GPA:", gpa_l),
        Paragraph(f"<b>{sgpa_2dp}</b>", gpa_v),
        Paragraph("Out of 4.00", gpa_o),
    ])
    rows.append([
        "", "", "",
        Paragraph("Cumulative GPA:", gpa_l),
        Paragraph(f"<b>{cgpa_2dp}</b>", gpa_v),
        Paragraph("Out of 4.00", gpa_o),
    ])

    ct = Table(rows, colWidths=col_w)
    ct.setStyle(TableStyle([
        # header
        ("BACKGROUND",    (0,0),(-1,0),            DGRAY),
        ("FONTNAME",      (0,0),(-1,0),            "Helvetica-Bold"),
        # grid only for subject rows (header + data)
        ("GRID",          (0,0),(-1,n_data),        0.5, BLACK),
        ("ALIGN",         (0,0),(-1,-1),            "CENTER"),
        ("VALIGN",        (0,0),(-1,-1),            "MIDDLE"),
        ("ALIGN",         (2,0),(2,-1),             "LEFT"),   # name left
        ("TOPPADDING",    (0,0),(-1,-1),            4),
        ("BOTTOMPADDING", (0,0),(-1,-1),            4),
        # GPA rows: span cols 0-2
        ("SPAN",          (0,n_data+1),(2,n_data+1)),
        ("SPAN",          (0,n_data+2),(2,n_data+2)),
        ("LINEABOVE",     (0,n_data+1),(-1,n_data+1), 0.5, BLACK),
        ("ALIGN",         (3,n_data+1),(3,-1),      "RIGHT"),
    ]))
    story.append(ct)
    story.append(Spacer(1, 14))

    # ── CLASS POSITION ──────────────────────────────────────────────────────
    story.append(Paragraph("2.&nbsp;&nbsp; His/ Her class position is:", body))
    pos = Table([
        [Paragraph("a.", body), Paragraph("Current Semester:", body),
         Paragraph(f"<b>{current_pos}</b>", b_bold), Paragraph("out of", body),
         Paragraph(f"<b>{total_students}</b>", b_bold)],
        [Paragraph("b.", body), Paragraph("Over All Semester:", body),
         Paragraph(f"<b>{overall_pos}</b>", b_bold), Paragraph("out of", body),
         Paragraph(f"<b>{total_students}</b>", b_bold)],
    ], colWidths=[0.8*cm, 4.2*cm, 1.4*cm, 1.4*cm, 1.4*cm])
    pos.setStyle(TableStyle([
        ("LEFTPADDING",   (0,0),(0,-1), 4),
        ("TOPPADDING",    (0,0),(-1,-1), 2),
        ("BOTTOMPADDING", (0,0),(-1,-1), 2),
    ]))
    story.append(pos)
    story.append(Spacer(1, 8))
    story.append(Paragraph(f"3.&nbsp;&nbsp; Academic Status: &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; <b>{acad_stat}</b>", body))
    story.append(Spacer(1, 28))

    # ── SIGNATURE BLOCK ─────────────────────────────────────────────────────
    sig = Table([
        ["", "_" * 26],
        ["", Paragraph(examiner_title, sig_c)],
        ["", ""],
        ["", Paragraph("_" * 24, sig_c)],
        ["", Paragraph("(Stamp)", sig_c)],
    ], colWidths=[CW*0.45, CW*0.55])
    sig.setStyle(TableStyle([
        ("ALIGN",         (1,0),(1,-1), "CENTER"),
        ("TOPPADDING",    (0,0),(-1,-1), 2),
        ("BOTTOMPADDING", (0,0),(-1,-1), 2),
    ]))
    story.append(sig)
    story.append(Spacer(1, 14))
    story.append(HRFlowable(width="100%", thickness=0.5, color=GRAY))
    story.append(Spacer(1, 4))
    story.append(Paragraph(
        "<i>This result is issued subject to the rectification of any error or omission "
        "as and when detected.</i>", foot))

    doc.build(story)
    print(f"✓ Student card: {output_path}")


# ═══════════════════════════════════════════════════════════════════════════════
# 2.  CLASS RESULT SHEET  (landscape spreadsheet)
# ═══════════════════════════════════════════════════════════════════════════════
def generate_class_result_sheet(
    output_path,
    batch_name     = "BCS-9",
    semester_no    = 3,
    semester_label = "Spring 2025",
    program        = "BS Computer Science",
    university     = "Dr. A. Q. Khan Institute of Computer Sciences & Information Technology",
    subjects       = None,  # [{code,name,credit_hours,type}]
    students       = None,  # [{name,reg_no,grades:{code:grade},previous_sgpas:[]}]
):
    subjects = subjects or []
    students = students or []

    PAGE = landscape(A4)
    LM = RM = 0.7*cm
    CW = PAGE[0] - LM - RM

    # ── pre-compute ──────────────────────────────────────────────────────────
    computed = []
    for st in students:
        sub_rows = [{
            "code":         s["code"],
            "credit_hours": s["credit_hours"],
            "grade":        st["grades"].get(s["code"], ""),
        } for s in subjects]
        sgpa_raw   = calc_sgpa(sub_rows)
        prev       = st.get("previous_sgpas", [])
        cgpa_raw   = calc_cgpa(prev + [sgpa_raw])
        computed.append({
            "name":     st["name"],
            "reg_no":   st.get("reg_no",""),
            "sub_rows": sub_rows,
            "sgpa_9":   sgpa_raw,
            "cgpa_9":   cgpa_raw,
            "sgpa_2":   round(sgpa_raw, 2),
            "cgpa_2":   round(cgpa_raw, 2),
            "status":   academic_status(cgpa_raw),
        })

    doc = SimpleDocTemplate(output_path, pagesize=PAGE,
                            leftMargin=LM, rightMargin=RM,
                            topMargin=1.0*cm, bottomMargin=1.2*cm)

    # tiny styles
    TH = S("TH", fontSize=6,   fontName="Helvetica-Bold", alignment=TA_CENTER, leading=8)
    TC = S("TC", fontSize=6.5, fontName="Helvetica",       alignment=TA_CENTER, leading=8)
    TL = S("TL", fontSize=6.5, fontName="Helvetica",       alignment=TA_LEFT,   leading=8)
    TB = S("TB", fontSize=6.5, fontName="Helvetica-Bold",  alignment=TA_CENTER, leading=8)
    T5 = S("T5", fontSize=5.5, fontName="Helvetica",       alignment=TA_CENTER, leading=7)

    story = []

    # page header
    story.append(Paragraph(university,
        S("uh", fontSize=10, fontName="Helvetica-Bold", alignment=TA_CENTER, spaceAfter=2)))
    story.append(Paragraph(
        f"{program} &nbsp;|&nbsp; {batch_name} &nbsp;|&nbsp; Semester {semester_no} "
        f"&nbsp;|&nbsp; {semester_label} &nbsp;|&nbsp; Class Result Sheet",
        S("us", fontSize=8, fontName="Helvetica", alignment=TA_CENTER, spaceAfter=0, textColor=GRAY)))
    story.append(Spacer(1, 5))
    story.append(HRFlowable(width="100%", thickness=0.8, color=BLACK, spaceAfter=5))

    # ── column widths ────────────────────────────────────────────────────────
    W_NO   = 0.65*cm
    W_NAME = 3.4*cm
    W_SGPA9= 2.4*cm
    W_SGPA2= 1.4*cm
    W_CGPA9= 2.4*cm
    W_CGPA2= 1.4*cm
    W_STAT = 2.8*cm

    n_s    = len(subjects)
    fixed  = W_NO + W_NAME + W_SGPA9 + W_SGPA2 + W_CGPA9 + W_CGPA2 + W_STAT
    avail  = CW - fixed
    W_PAIR = avail / n_s if n_s else 1.4*cm
    W_GR   = W_PAIR * 0.52
    W_GP   = W_PAIR * 0.48

    col_w = [W_NO, W_NAME] + [W_GR, W_GP]*n_s + [W_SGPA9, W_SGPA2, W_CGPA9, W_CGPA2, W_STAT]

    # ── 4 header rows ────────────────────────────────────────────────────────
    def hcell(t): return Paragraph(t, TH)

    # Row 0: subject names (span grade+gp)
    r0 = [hcell("No."), hcell("Name")]
    for s in subjects:
        r0 += [hcell(f"{s['name']}\n({s['type'][0]})"), ""]
    r0 += [hcell("SGPA\n(9 decimal\nplaces)"),
           hcell("SGPA\n(rounded\nto 2dp)"),
           hcell("CGPA\n(9 decimal\nplaces)"),
           hcell("CGPA\n(rounded\nto 2dp)"),
           hcell("Academic\nStatus")]

    # Row 1: credit hours
    r1 = ["", ""]
    for s in subjects: r1 += [hcell(f"Credit Hrs\n{s['credit_hours']}"), ""]
    r1 += ["", "", "", "", ""]

    # Row 2: course code
    r2 = ["", ""]
    for s in subjects: r2 += [hcell(s["code"]), ""]
    r2 += ["", "", "", "", ""]

    # Row 3: Grade / Grade Points
    r3 = [hcell("No."), hcell("Name")]
    for _ in subjects: r3 += [hcell("Grade"), hcell("Grade\nPoints")]
    r3 += ["", "", "", "", ""]

    rows = [r0, r1, r2, r3]

    # ── data rows ─────────────────────────────────────────────────────────────
    for idx, st in enumerate(computed, 1):
        row = [Paragraph(str(idx), TC), Paragraph(st["name"], TL)]
        for sr in st["sub_rows"]:
            g  = sr["grade"]
            gv = f"{gp(g):.2f}" if g else ""
            row += [Paragraph(g, TC), Paragraph(gv, TC)]
        # SGPA 9dp
        sgpa9_str = f"{st['sgpa_9']:.9f}"
        cgpa9_str = f"{st['cgpa_9']:.9f}"
        row += [
            Paragraph(sgpa9_str,           T5),
            Paragraph(f"{st['sgpa_2']:.2f}", TB),
            Paragraph(cgpa9_str,           T5),
            Paragraph(f"{st['cgpa_2']:.2f}", TB),
            Paragraph(st["status"], S("SS", fontSize=6, fontName="Helvetica-Bold",
                                       alignment=TA_CENTER, leading=8,
                                       textColor=_sc(st["status"]))),
        ]
        rows.append(row)

    # ── build table ──────────────────────────────────────────────────────────
    tbl = Table(rows, colWidths=col_w, repeatRows=4)
    n_hdr = 4
    n_tot = len(rows)

    cmds = [
        ("GRID",          (0,0),(-1,-1),           0.3, colors.HexColor("#888")),
        ("BACKGROUND",    (0,0),(-1,n_hdr-1),      DGRAY),
        ("FONTNAME",      (0,0),(-1,n_hdr-1),      "Helvetica-Bold"),
        ("ALIGN",         (0,0),(-1,-1),            "CENTER"),
        ("VALIGN",        (0,0),(-1,-1),            "MIDDLE"),
        ("ALIGN",         (1,0),(1,-1),             "LEFT"),
        ("TOPPADDING",    (0,0),(-1,-1),            1),
        ("BOTTOMPADDING", (0,0),(-1,-1),            1),
        ("LEFTPADDING",   (0,0),(-1,-1),            1),
        ("RIGHTPADDING",  (0,0),(-1,-1),            1),
    ]
    # span No & Name rows 0-3
    for col in (0, 1):
        cmds.append(("SPAN",(col,0),(col,3)))
    # span subject pair in rows 0-2
    for i in range(n_s):
        pc = 2 + i*2
        for hr in (0,1,2):
            cmds.append(("SPAN",(pc,hr),(pc+1,hr)))
    # span fixed right cols rows 0-3
    fc = 2 + n_s*2
    for c in range(5):
        cmds.append(("SPAN",(fc+c,0),(fc+c,3)))
    # alternating row bg
    for r in range(n_hdr, n_tot):
        if (r-n_hdr)%2==1:
            cmds.append(("BACKGROUND",(0,r),(-1,r),colors.HexColor("#F5F5F5")))

    tbl.setStyle(TableStyle(cmds))
    story.append(tbl)

    # ── footer / signature ──────────────────────────────────────────────────
    story.append(Spacer(1,14))
    story.append(HRFlowable(width="100%", thickness=0.5, color=GRAY))
    story.append(Spacer(1,6))
    SIG = S("SIG", fontSize=8, fontName="Helvetica", alignment=TA_CENTER)
    sig = Table([
        [Paragraph("_"*28, SIG), Paragraph("_"*28, SIG), Paragraph("_"*28, SIG)],
        [Paragraph("Prepared By", SIG),
         Paragraph("Verified By (HOD)", SIG),
         Paragraph("Deputy Controller of Examinations", SIG)],
        ["","",Paragraph("(Official Stamp)", SIG)],
    ], colWidths=[CW/3]*3)
    sig.setStyle(TableStyle([
        ("ALIGN",(0,0),(-1,-1),"CENTER"),
        ("TOPPADDING",(0,0),(-1,-1),4),
        ("BOTTOMPADDING",(0,0),(-1,-1),4),
    ]))
    story.append(sig)
    story.append(Spacer(1,5))
    story.append(Paragraph(
        "<i>This result is issued subject to the rectification of any error or omission as and when detected.</i>",
        S("fn", fontSize=7, fontName="Helvetica-Oblique", alignment=TA_CENTER, textColor=GRAY)))

    doc.build(story)
    print(f"✓ Class sheet: {output_path}")


def _sc(s):
    return {"Excellent":colors.HexColor("#155724"),
            "Very Good":colors.HexColor("#1A5276"),
            "Good":colors.HexColor("#186A3B"),
            "Satisfactory":colors.HexColor("#145A32"),
            "Fair":colors.HexColor("#784212"),
            "Warning":colors.HexColor("#7D6608"),
            "Extended Temporary Enrollment":colors.HexColor("#922B21")}.get(s,BLACK)


# ═══════════════════════════════════════════════════════════════════════════════
# DEMO
# ═══════════════════════════════════════════════════════════════════════════════
if __name__ == "__main__":
    os.makedirs("/mnt/user-data/outputs", exist_ok=True)

    # ── Individual card: matches Image 1 exactly ──────────────────────────────
    SALEH_SUBS = [
        {"code":"227106","name":"Computer Organization & Assembly Language",      "credit_hours":2,"grade":"B"},
        {"code":"227107","name":"Computer Organization & Assembly Language (Lab)","credit_hours":1,"grade":"A-"},
        {"code":"227105","name":"Theory of Automata",                             "credit_hours":3,"grade":"A"},
        {"code":"227305","name":"Advanced Database Systems",                      "credit_hours":2,"grade":"B+"},
        {"code":"227306","name":"Advanced Database Systems (Lab)",                "credit_hours":1,"grade":"B"},
        {"code":"217409","name":"Applied Physics",                                "credit_hours":2,"grade":"B-"},
        {"code":"217410","name":"Applied Physics (Lab)",                          "credit_hours":1,"grade":"A-"},
        {"code":"200307","name":"Expository Writing",                             "credit_hours":3,"grade":"A"},
        {"code":"100138","name":"Islamic Studies",                                "credit_hours":2,"grade":"A-"},
    ]
    # verify: SGPA should be 3.51, CGPA 3.28
    sgpa_check = calc_sgpa(SALEH_SUBS)
    print(f"Saleh SGPA check: {sgpa_check:.9f} → {round(sgpa_check,2):.2f}  (expected ~3.51)")
    prev3 = [3.10, 3.20, 3.25]
    cgpa_check = calc_cgpa(prev3 + [sgpa_check])
    print(f"Saleh CGPA check: {cgpa_check:.9f} → {round(cgpa_check,2):.2f}  (expected ~3.28)")

    generate_student_result_card(
        output_path    = "/mnt/user-data/outputs/StudentResult_MuhammadSalehHayat.pdf",
        parent_name    = "Gul Nisar",
        student_name   = "Muhammad Saleh Hayat",
        reg_no         = "232201070",
        class_name     = "BCS-9",
        semester_label = "4th Semester",
        result_title   = "Result – Spring – 2025",
        subjects       = SALEH_SUBS,
        previous_sgpas = prev3,
        current_pos=14, overall_pos=23, total_students=83,
    )

    # ── Class sheet: matches Image 2 ─────────────────────────────────────────
    SUBJS = [
        {"code":"CS301","name":"Object Oriented Programming (Theory)", "credit_hours":3,"type":"Theory"},
        {"code":"CS302","name":"Object Oriented Programming (Lab)",    "credit_hours":1,"type":"Lab"},
        {"code":"CS303","name":"Database Systems (Theory)",            "credit_hours":3,"type":"Theory"},
        {"code":"CS304","name":"Database Systems (Lab)",               "credit_hours":1,"type":"Lab"},
        {"code":"CS305","name":"Digital Logic Design (Theory)",        "credit_hours":2,"type":"Theory"},
        {"code":"CS306","name":"Digital Logic Design (Lab)",           "credit_hours":1,"type":"Lab"},
        {"code":"MATH1","name":"Multivariable Calculus",               "credit_hours":3,"type":"Theory"},
        {"code":"MATH2","name":"Linear Algebra",                       "credit_hours":3,"type":"Theory"},
        {"code":"HU301","name":"Additional Mathematics-II",            "credit_hours":3,"type":"Theory"},
    ]
    STUDS = [
        {"name":"Muddassr Ishfaq",          "reg_no":"001",
         "grades":{"CS301":"D+","CS302":"A-","CS303":"C+","CS304":"D+","CS305":"C","CS306":"C","MATH1":"C+","MATH2":"D+","HU301":"C+"},
         "previous_sgpas":[1.85,2.10]},
        {"name":"Haider Ali",               "reg_no":"002",
         "grades":{"CS301":"C","CS302":"B+","CS303":"B+","CS304":"C","CS305":"B","CS306":"C","MATH1":"B-","MATH2":"D+","HU301":"C+"},
         "previous_sgpas":[2.20,2.45]},
        {"name":"Muhammad Harib",           "reg_no":"003",
         "grades":{"CS301":"B-","CS302":"B+","CS303":"A-","CS304":"C","CS305":"B","CS306":"C+","MATH1":"C+","MATH2":"D+","HU301":"C+"},
         "previous_sgpas":[2.50,2.70]},
        {"name":"Muhammad Saleh Hayat",     "reg_no":"232201070",
         "grades":{"CS301":"B","CS302":"A-","CS303":"A-","CS304":"C+","CS305":"A-","CS306":"C-","MATH1":"C-","MATH2":"C","HU301":"C"},
         "previous_sgpas":[2.60,2.80]},
        {"name":"Mehwish Rauf",             "reg_no":"005",
         "grades":{"CS301":"B+","CS302":"A","CS303":"A","CS304":"B-","CS305":"A","CS306":"A","MATH1":"A","MATH2":"A","HU301":"A"},
         "previous_sgpas":[3.60,3.75]},
        {"name":"Um-e-Nayab",               "reg_no":"006",
         "grades":{"CS301":"C+","CS302":"A-","CS303":"A","CS304":"B-","CS305":"B","CS306":"A-","MATH1":"B-","MATH2":"A","HU301":"A"},
         "previous_sgpas":[3.30,3.45]},
        {"name":"Zaigham Shahid Raja",      "reg_no":"007",
         "grades":{"CS301":"D","CS302":"A-","CS303":"B-","CS304":"C-","CS305":"C","CS306":"B","MATH1":"D","MATH2":"D","HU301":"C"},
         "previous_sgpas":[1.60,1.80]},
        {"name":"Amna Gul",                 "reg_no":"008",
         "grades":{"CS301":"C+","CS302":"A","CS303":"A-","CS304":"D+","CS305":"B+","CS306":"B+","MATH1":"A-","MATH2":"B","HU301":""},
         "previous_sgpas":[2.70,2.90]},
        {"name":"Nayyab Gul",               "reg_no":"009",
         "grades":{"CS301":"A-","CS302":"A","CS303":"A","CS304":"B+","CS305":"A","CS306":"A-","MATH1":"A","MATH2":"A","HU301":"A"},
         "previous_sgpas":[3.75,3.82]},
        {"name":"Syed Ali Murtajiz Bukhari","reg_no":"232201086",
         "grades":{"CS301":"D","CS302":"B+","CS303":"C","CS304":"C-","CS305":"B","CS306":"A","MATH1":"F","MATH2":"","HU301":"C-"},
         "previous_sgpas":[1.80,2.00]},
        {"name":"Muhammad Muneeb",          "reg_no":"232201093",
         "grades":{"CS301":"D","CS302":"B+","CS303":"A-","CS304":"C+","CS305":"C-","CS306":"B-","MATH1":"D+","MATH2":"D+","HU301":"D+"},
         "previous_sgpas":[1.90,2.05]},
        {"name":"Bilal",                    "reg_no":"010",
         "grades":{"CS301":"D+","CS302":"B+","CS303":"A","CS304":"C-","CS305":"C","CS306":"C+","MATH1":"F","MATH2":"D","HU301":""},
         "previous_sgpas":[1.70,1.82]},
        {"name":"Abdus Salam",              "reg_no":"011",
         "grades":{"CS301":"C","CS302":"A-","CS303":"C+","CS304":"C","CS305":"B-","CS306":"D+","MATH1":"D","MATH2":"C-","HU301":"C"},
         "previous_sgpas":[1.95,2.15]},
        {"name":"Sunila Aziz",              "reg_no":"012",
         "grades":{"CS301":"D+","CS302":"B+","CS303":"B","CS304":"C","CS305":"B-","CS306":"C","MATH1":"C","MATH2":"C","HU301":"C"},
         "previous_sgpas":[2.10,2.25]},
        {"name":"Maira Bibi",               "reg_no":"013",
         "grades":{"CS301":"A-","CS302":"A-","CS303":"A","CS304":"B-","CS305":"A","CS306":"B","MATH1":"A","MATH2":"B","HU301":"A"},
         "previous_sgpas":[3.40,3.50]},
    ]
    generate_class_result_sheet(
        output_path    = "/mnt/user-data/outputs/ClassResult_BCS9_Semester3.pdf",
        batch_name     = "BCS-9", semester_no=3,
        semester_label = "Spring 2025",
        program        = "BS Computer Science",
        subjects       = SUBJS, students=STUDS,
    )
