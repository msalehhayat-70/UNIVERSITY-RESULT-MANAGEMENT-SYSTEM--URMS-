"""
KICSIT Result PDF Generator  v4
Changes from v3:
  - Class sheet: Father Name column added (after Reg No, before Name)
  - Class sheet: Registration No column confirmed
  - Class sheet: "Prepared by" moved to left bottom (matching real sheet)
  - Class sheet: JE (Exams) signature added bottom left
  - Class sheet: Deputy Controller signature bottom right
  - Student card: all v3 fixes retained
  - 50 students per page with page break
"""
from reportlab.lib.pagesizes import A4, landscape
from reportlab.lib import colors
from reportlab.lib.units import cm
from reportlab.lib.styles import ParagraphStyle
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle,
    HRFlowable, PageBreak
)
from reportlab.lib.enums import TA_CENTER, TA_LEFT, TA_RIGHT
import os

BLACK = colors.black
WHITE = colors.white
GRAY  = colors.HexColor("#555555")
LGRAY = colors.HexColor("#BBBBBB")
DGRAY = colors.HexColor("#D0D0D0")

GRADES = {
    "A":  4.00,"A-": 3.67,
    "B+": 3.33,"B":  3.00,"B-": 2.67,
    "C+": 2.33,"C":  2.00,"C-": 1.67,
    "D+": 1.33,"D":  1.00,
    "F":  0.00,"W/C":0.00,"":   0.00,
}

def gp(grade): return GRADES.get(str(grade).strip(), 0.00)

def calc_sgpa(subjects):
    num = sum(gp(s["grade"]) * s["credit_hours"] for s in subjects)
    den = sum(s["credit_hours"] for s in subjects)
    return num / den if den else 0.0

def calc_cgpa(sgpa_list):
    return sum(sgpa_list)/len(sgpa_list) if sgpa_list else 0.0

def academic_status(cgpa):
    if cgpa >= 3.50: return "Excellent"
    if cgpa >= 3.00: return "Very Good"
    if cgpa >= 2.50: return "Good"
    if cgpa >= 2.00: return "Satisfactory"
    if cgpa >= 1.50: return "Fair"
    if cgpa >= 1.00: return "Warning"
    return "Extended Temporary Enrollment"

def P(text, style): return Paragraph(str(text), style)
def S(name, **kw):  return ParagraphStyle(name, **kw)

def logo_cell(label, size):
    t = Table([[P(f"<b>{label}</b>",
        S("lbl",fontSize=7,fontName="Helvetica-Bold",alignment=TA_CENTER,textColor=LGRAY))]],
        colWidths=[size], rowHeights=[size])
    t.setStyle(TableStyle([
        ("BOX",   (0,0),(0,0), 1, LGRAY),
        ("ALIGN", (0,0),(0,0), "CENTER"),
        ("VALIGN",(0,0),(0,0), "MIDDLE"),
        ("TOPPADDING",   (0,0),(0,0), 0),
        ("BOTTOMPADDING",(0,0),(0,0), 0),
    ]))
    return t

def status_color(s):
    return {"Excellent":"#155724","Very Good":"#1A5276","Good":"#186A3B",
            "Satisfactory":"#145A32","Fair":"#784212","Warning":"#7D6608",
            "Extended Temporary Enrollment":"#922B21"}.get(s,"#000000")

# ═══════════════════════════════════════════════════════════════════════════════
#  1. INDIVIDUAL STUDENT RESULT CARD  — one page per student
# ═══════════════════════════════════════════════════════════════════════════════
def generate_student_result_card(
    output_path,
    parent_name    = "Gul Nisar",
    student_name   = "Muhammad Saleh Hayat",
    reg_no         = "232201070",
    class_name     = "BCS-9",
    semester_label = "4th Semester",
    result_title   = "Result – Spring – 2025",
    subjects       = None,
    previous_sgpas = None,
    current_pos    = 14,
    overall_pos    = 23,
    total_students = 83,
    examiner_name  = "Faheem Ahmed",
    examiner_title = "Deputy Controller of Examinations KICSIT",
):
    subjects       = subjects or []
    previous_sgpas = previous_sgpas or []

    sgpa_raw  = calc_sgpa(subjects)
    all_sgpas = previous_sgpas + [sgpa_raw]
    cgpa_raw  = calc_cgpa(all_sgpas)
    sgpa_2dp  = f"{round(sgpa_raw,2):.2f}"
    cgpa_2dp  = f"{round(cgpa_raw,2):.2f}"
    acad_stat = academic_status(cgpa_raw)

    LM = RM = 2.0*cm
    CW = A4[0] - LM - RM

    doc = SimpleDocTemplate(output_path, pagesize=A4,
        leftMargin=LM, rightMargin=RM, topMargin=1.2*cm, bottomMargin=1.0*cm)

    uni_s  = S("uni", fontSize=13,fontName="Helvetica-Bold",  alignment=TA_CENTER,leading=17,spaceAfter=1)
    cam_s  = S("cam", fontSize=11,fontName="Helvetica-Bold",  alignment=TA_CENTER,leading=14,spaceAfter=1)
    adr_s  = S("adr", fontSize=8, fontName="Helvetica",       alignment=TA_CENTER,textColor=GRAY,spaceAfter=1)
    web_s  = S("web", fontSize=8, fontName="Helvetica",       alignment=TA_CENTER,textColor=colors.blue)
    body_s = S("body",fontSize=10,fontName="Helvetica",       leading=14,spaceAfter=2)
    bold_s = S("bld", fontSize=10,fontName="Helvetica-Bold",  leading=14)
    th_s   = S("th",  fontSize=9, fontName="Helvetica-Bold",  alignment=TA_CENTER,leading=11)
    tdc_s  = S("tdc", fontSize=9, fontName="Helvetica",       alignment=TA_CENTER,leading=11)
    tdl_s  = S("tdl", fontSize=9, fontName="Helvetica",       alignment=TA_LEFT,  leading=12)
    gpal_s = S("gl",  fontSize=9, fontName="Helvetica-Bold",  alignment=TA_RIGHT, leading=13)
    gpav_s = S("gv",  fontSize=9, fontName="Helvetica-Bold",  alignment=TA_LEFT,  leading=13)
    sig_s  = S("sig", fontSize=9, fontName="Helvetica",       alignment=TA_CENTER,leading=12)
    foot_s = S("ft",  fontSize=8, fontName="Helvetica-Oblique",alignment=TA_LEFT, textColor=GRAY)

    story = []

    # ── HEADER ────────────────────────────────────────────────────────────────
    LOGO_W = 1.6*cm
    TEXT_W = CW - 2*LOGO_W
    txt_blk = Table([
        [P("Dr. A. Q. Khan Institute of Computer Sciences &amp;<br/>Information Technology", uni_s)],
        [P("Campus of Institute of Space Technology, Islamabad", cam_s)],
        [P("KRL Kahuta, Distt. Rawalpindi, Pakistan.  Tel +92.51.9285059, Fax +92.51.9285245", adr_s)],
        [P('<u>www.kicsit.edu.pk</u>', web_s)],
    ], colWidths=[TEXT_W])
    txt_blk.setStyle(TableStyle([("TOPPADDING",(0,0),(-1,-1),1),("BOTTOMPADDING",(0,0),(-1,-1),1)]))

    hdr = Table([[logo_cell("ist",LOGO_W), txt_blk, logo_cell("KICSIT",LOGO_W)]],
                colWidths=[LOGO_W, TEXT_W, LOGO_W])
    hdr.setStyle(TableStyle([
        ("VALIGN",(0,0),(-1,-1),"MIDDLE"),
        ("TOPPADDING",(0,0),(-1,-1),0),
        ("BOTTOMPADDING",(0,0),(-1,-1),0),
    ]))
    story.append(hdr)
    story.append(HRFlowable(width="100%",thickness=1.2,color=BLACK,spaceAfter=5))

    # ── TO / SUBJECT ──────────────────────────────────────────────────────────
    story.append(P("To:", body_s))
    story.append(P(f"&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<b>{parent_name}</b>", body_s))
    story.append(Spacer(1,4))
    story.append(P(f"Subject:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; <b><u>{result_title}</u></b>", body_s))
    story.append(Spacer(1,6))
    story.append(P("Dear Parents/Guardians", body_s))
    story.append(Spacer(1,3))
    story.append(P(
        f"1.&nbsp;&nbsp; Your son/daughter/ward <b><u>{student_name}</u></b> "
        f"Registration No. <b><u>{reg_no}</u></b> of Class <b><u>{class_name}</u></b> has "
        f"scored the following grades in the &nbsp;<u>{semester_label}</u>&nbsp; exams.", body_s))
    story.append(Spacer(1,6))

    # ── COURSE TABLE (5 cols, no empty col) ──────────────────────────────────
    W0=1.0*cm; W1=2.4*cm; W3=1.9*cm; W4=1.6*cm
    W2=CW-W0-W1-W3-W4

    tbl_rows = [[P("S.<br/>No.",th_s),P("Course<br/>Code",th_s),
                 P("Course Name",th_s),P("Credit<br/>Hour",th_s),P("Grade",th_s)]]
    for i,s in enumerate(subjects,1):
        tbl_rows.append([P(str(i),tdc_s),P(s["code"],tdc_s),
                         P(s["name"],tdl_s),P(str(s["credit_hours"]),tdc_s),P(s["grade"],tdc_s)])
    n = len(subjects)

    # GPA inline sub-table spans cols 2-4
    gpa_sub = Table([
        [P("<b>Semester GPA:</b>",gpal_s), P(f"<b>{sgpa_2dp}</b>",gpav_s), P("Out of 4.00",gpav_s)],
        [P("<b>Cumulative GPA:</b>",gpal_s),P(f"<b>{cgpa_2dp}</b>",gpav_s),P("Out of 4.00",gpav_s)],
    ], colWidths=[3.0*cm,1.4*cm,2.3*cm])
    gpa_sub.setStyle(TableStyle([
        ("TOPPADDING",(0,0),(-1,-1),3),("BOTTOMPADDING",(0,0),(-1,-1),3),
        ("LEFTPADDING",(0,0),(-1,-1),2),("RIGHTPADDING",(0,0),(-1,-1),2),
    ]))
    tbl_rows.append(["","",gpa_sub,"",""])

    ct = Table(tbl_rows, colWidths=[W0,W1,W2,W3,W4])
    ct.setStyle(TableStyle([
        ("BACKGROUND",(0,0),(-1,0), DGRAY),
        ("FONTNAME",  (0,0),(-1,0), "Helvetica-Bold"),
        ("GRID",      (0,0),(-1,n), 0.5,BLACK),
        ("ALIGN",     (0,0),(-1,-1),"CENTER"),
        ("VALIGN",    (0,0),(-1,-1),"MIDDLE"),
        ("ALIGN",     (2,1),(2,n),  "LEFT"),
        ("TOPPADDING",(0,0),(-1,-1),3),("BOTTOMPADDING",(0,0),(-1,-1),3),
        ("LEFTPADDING",(2,1),(2,n), 4),
        ("SPAN",      (0,n+1),(1,n+1)),
        ("SPAN",      (2,n+1),(4,n+1)),
        ("ALIGN",     (2,n+1),(4,n+1),"RIGHT"),
        ("TOPPADDING",(0,n+1),(-1,n+1),0),
        ("BOTTOMPADDING",(0,n+1),(-1,n+1),0),
        ("LEFTPADDING",(0,n+1),(-1,n+1),0),
        ("RIGHTPADDING",(0,n+1),(-1,n+1),0),
    ]))
    story.append(ct)
    story.append(Spacer(1,8))

    # ── CLASS POSITION ────────────────────────────────────────────────────────
    story.append(P("2.&nbsp;&nbsp; His/ Her class position is:", body_s))
    pos = Table([
        ["",P("a.",body_s),P("Current Semester:",body_s),
         P(f"<b>{current_pos}</b>",bold_s),P("out of",body_s),P(f"<b>{total_students}</b>",bold_s)],
        ["",P("b.",body_s),P("Over All Semester:",body_s),
         P(f"<b>{overall_pos}</b>",bold_s),P("out of",body_s),P(f"<b>{total_students}</b>",bold_s)],
    ], colWidths=[1.0*cm,0.7*cm,4.2*cm,1.3*cm,1.2*cm,1.3*cm])
    pos.setStyle(TableStyle([("TOPPADDING",(0,0),(-1,-1),2),("BOTTOMPADDING",(0,0),(-1,-1),2)]))
    story.append(pos)
    story.append(Spacer(1,5))
    story.append(P(f"3.&nbsp;&nbsp; Academic Status:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; <b>{acad_stat}</b>", body_s))
    story.append(Spacer(1,16))

    # ── SIGNATURE ────────────────────────────────────────────────────────────
    sig_tbl = Table([
        ["", P("_"*26, sig_s)],
        ["", P(examiner_name, sig_s)],
        ["", P(examiner_title, sig_s)],
    ], colWidths=[CW*0.42, CW*0.58])
    sig_tbl.setStyle(TableStyle([
        ("ALIGN",(1,0),(1,-1),"CENTER"),
        ("TOPPADDING",(0,0),(-1,-1),1),
        ("BOTTOMPADDING",(0,0),(-1,-1),1),
    ]))
    story.append(sig_tbl)
    story.append(Spacer(1,12))
    story.append(HRFlowable(width="100%",thickness=10,color=BLACK,spaceAfter=5))
    story.append(Spacer(1,3))
    story.append(P(
        "<i>This result is issued subject to the rectification of any error or "
        "omission as and when detected.</i>", foot_s))

    doc.build(story)
    print(f"✓  Student card → {output_path}")


# ═══════════════════════════════════════════════════════════════════════════════
#  2. CLASS RESULT SHEET  — landscape, Reg No + Father Name columns, 50/page
# ═══════════════════════════════════════════════════════════════════════════════
def generate_class_result_sheet(
    output_path,
    batch_name        = "BCS-9",
    semester_no       = 3,
    semester_label    = "Spring 2025",
    program           = "BS Computer Science",
    university        = "Dr. A. Q. Khan Institute of Computer Sciences & Information Technology",
    notification_no   = "",
    subjects          = None,
    students          = None,
    students_per_page = 50,
    je_name           = "Muhammad Latif",
    je_title          = "JE (Exams) KICSIT",
    hod_name          = "",
    examiner_name     = "Faheem Ahmed",
    examiner_title    = "Deputy Controller of Examinations KICSIT",
):
    subjects = subjects or []
    students  = students  or []

    # pre-compute
    computed = []
    for st in students:
        sr = [{"code":s["code"],"credit_hours":s["credit_hours"],
               "grade":st["grades"].get(s["code"],"")} for s in subjects]
        sgpa_raw = calc_sgpa(sr)
        prev     = st.get("previous_sgpas",[])
        cgpa_raw = calc_cgpa(prev+[sgpa_raw])
        computed.append({
            "name":       st["name"],
            "reg_no":     st.get("reg_no",""),
            "father":     st.get("father_name",""),
            "sr":         sr,
            "sgpa9":      sgpa_raw,
            "cgpa9":      cgpa_raw,
            "sgpa2":      round(sgpa_raw,2),
            "cgpa2":      round(cgpa_raw,2),
            "status":     academic_status(cgpa_raw),
        })

    PAGE = landscape(A4)
    LM=RM=0.55*cm; TM=BM=0.8*cm
    CW = PAGE[0]-LM-RM

    doc = SimpleDocTemplate(output_path, pagesize=PAGE,
        leftMargin=LM,rightMargin=RM,topMargin=TM,bottomMargin=BM)

    # styles
    TH = S("TH",fontSize=5.2,fontName="Helvetica-Bold",alignment=TA_CENTER,leading=6.5)
    TC = S("TC",fontSize=5.5,fontName="Helvetica",      alignment=TA_CENTER,leading=6.5)
    TL = S("TL",fontSize=5.5,fontName="Helvetica",      alignment=TA_LEFT,  leading=6.5)
    TB = S("TB",fontSize=6,  fontName="Helvetica-Bold", alignment=TA_CENTER,leading=7)
    T4 = S("T4",fontSize=4,  fontName="Helvetica",      alignment=TA_CENTER,leading=5.5)

    n_s = len(subjects)

    # ── column widths ─────────────────────────────────────────────────────────
    W_SN  = 0.50*cm
    W_REG = 1.85*cm
    W_FTH = 2.20*cm   # Father Name  ← NEW
    W_NM  = 2.80*cm
    W_S9  = 2.10*cm
    W_S2  = 1.20*cm
    W_C9  = 2.10*cm
    W_C2  = 1.20*cm
    W_ST  = 2.30*cm

    fixed = W_SN+W_REG+W_FTH+W_NM+W_S9+W_S2+W_C9+W_C2+W_ST
    avail = CW - fixed
    W_GR  = (avail/n_s)*0.52 if n_s else 0.9*cm
    W_GP  = (avail/n_s)*0.48 if n_s else 0.8*cm

    col_w = [W_SN,W_REG,W_FTH,W_NM]+[W_GR,W_GP]*n_s+[W_S9,W_S2,W_C9,W_C2,W_ST]

    def hc(t): return P(t,TH)

    def header_rows():
        # row 0: subject names (pair spanned)
        r0=[hc("S.<br/>No."),hc("Reg No"),hc("Father<br/>Name"),hc("Name")]
        for s in subjects: r0+=[hc(f"{s['name']}<br/>({s['type'][0]})"), ""]
        r0+=[hc("SGPA<br/>(9 decimal<br/>places)"),hc("SGPA<br/>(rounded<br/>to 2dp)"),
             hc("CGPA<br/>(9 decimal<br/>places)"),hc("CGPA<br/>(rounded<br/>to 2dp)"),
             hc("Academic<br/>Status")]
        # row 1: credit hours
        r1=["","","",""]
        for s in subjects: r1+=[hc(f"Cr.Hrs<br/>{s['credit_hours']}"),""]
        r1+=["","","","",""]
        # row 2: code
        r2=["","","",""]
        for s in subjects: r2+=[hc(s["code"]),""]
        r2+=["","","","",""]
        # row 3: Grade / GP
        r3=[hc("S.<br/>No."),hc("Reg No"),hc("Father<br/>Name"),hc("Name")]
        for _ in subjects: r3+=[hc("Grade"),hc("Grade<br/>Points")]
        r3+=["","","","",""]
        return [r0,r1,r2,r3]

    def data_row(idx, st):
        row=[P(str(idx),TC),P(st["reg_no"],TC),P(st["father"],TL),P(st["name"],TL)]
        for sr in st["sr"]:
            g=sr["grade"]; gv=f"{gp(g):.2f}" if g else ""
            row+=[P(g,TC),P(gv,TC)]
        sc=status_color(st["status"])
        row+=[
            P(f"{st['sgpa9']:.9f}",T4),
            P(f"{st['sgpa2']:.2f}",TB),
            P(f"{st['cgpa9']:.9f}",T4),
            P(f"{st['cgpa2']:.2f}",TB),
            P(st["status"],S(f"sc{idx}",fontSize=5.2,fontName="Helvetica-Bold",
              alignment=TA_CENTER,leading=6.5,textColor=colors.HexColor(sc))),
        ]
        return row

    def tbl_styles(n_hdr,n_rows):
        cmds=[
            ("GRID",       (0,0),(-1,-1),         0.25,colors.HexColor("#999")),
            ("BACKGROUND", (0,0),(-1,n_hdr-1),    DGRAY),
            ("FONTNAME",   (0,0),(-1,n_hdr-1),    "Helvetica-Bold"),
            ("ALIGN",      (0,0),(-1,-1),          "CENTER"),
            ("VALIGN",     (0,0),(-1,-1),          "MIDDLE"),
            ("ALIGN",      (2,0),(3,-1),           "LEFT"),   # Father + Name left
            ("TOPPADDING", (0,0),(-1,-1),          1),
            ("BOTTOMPADDING",(0,0),(-1,-1),        1),
            ("LEFTPADDING",(0,0),(-1,-1),          1),
            ("RIGHTPADDING",(0,0),(-1,-1),         1),
        ]
        # span S.No, Reg, Father, Name rows 0-3
        for col in (0,1,2,3): cmds.append(("SPAN",(col,0),(col,3)))
        # span subject pairs rows 0-2
        for i in range(n_s):
            pc=4+i*2
            for hr in (0,1,2): cmds.append(("SPAN",(pc,hr),(pc+1,hr)))
        # span fixed right cols rows 0-3
        fc=4+n_s*2
        for c in range(5): cmds.append(("SPAN",(fc+c,0),(fc+c,3)))
        # alternating
        for r in range(n_hdr, n_hdr+n_rows):
            if (r-n_hdr)%2==1:
                cmds.append(("BACKGROUND",(0,r),(-1,r),colors.HexColor("#F5F5F5")))
        return cmds

    def page_hdr(notif=""):
        items=[
            P(university,S("uh",fontSize=9,fontName="Helvetica-Bold",alignment=TA_CENTER,spaceAfter=1)),
            P(f"{program} &nbsp;|&nbsp; {batch_name} &nbsp;|&nbsp; Semester {semester_no} "
              f"&nbsp;|&nbsp; {semester_label} &nbsp;|&nbsp; Class Result Sheet",
              S("us",fontSize=7,fontName="Helvetica",alignment=TA_CENTER,textColor=GRAY,spaceAfter=0)),
        ]
        if notif:
            items.append(P(f"Notification No: {notif}",
                S("nn",fontSize=6.5,fontName="Helvetica",alignment=TA_CENTER,textColor=GRAY,spaceAfter=0)))
        items+=[Spacer(1,3),HRFlowable(width="100%",thickness=0.8,color=BLACK,spaceAfter=3)]
        return items

    def footer_block():
        SG=S("sg",fontSize=7,fontName="Helvetica",alignment=TA_CENTER)
        SB=S("sb",fontSize=7,fontName="Helvetica-Bold",alignment=TA_CENTER)
        # Left: Prepared by + JE signature | Right: Deputy Controller
        left_col = Table([
            [P("Prepared by",SB)],
            [Spacer(1,16)],
            [P("_"*22,SG)],
            [P(je_name,SG)],
            [P(je_title,SG)],
        ], colWidths=[CW*0.40])
        left_col.setStyle(TableStyle([
            ("ALIGN",(0,0),(-1,-1),"LEFT"),
            ("TOPPADDING",(0,0),(-1,-1),2),
            ("BOTTOMPADDING",(0,0),(-1,-1),1),
        ]))
        right_col = Table([
            [Spacer(1,16)],
            [P("_"*24,SG)],
            [P(examiner_name,SG)],
            [P(examiner_title,SG)],
        ], colWidths=[CW*0.45])
        right_col.setStyle(TableStyle([
            ("ALIGN",(0,0),(-1,-1),"CENTER"),
            ("TOPPADDING",(0,0),(-1,-1),2),
            ("BOTTOMPADDING",(0,0),(-1,-1),1),
        ]))
        wrapper = Table([[left_col,"",right_col]],
                        colWidths=[CW*0.40, CW*0.15, CW*0.45])
        wrapper.setStyle(TableStyle([
            ("VALIGN",(0,0),(-1,-1),"BOTTOM"),
            ("TOPPADDING",(0,0),(-1,-1),0),
            ("BOTTOMPADDING",(0,0),(-1,-1),0),
        ]))
        return wrapper

    # ── paginate ──────────────────────────────────────────────────────────────
    story=[]
    chunks=[computed[i:i+students_per_page] for i in range(0,len(computed),students_per_page)]

    for pidx,chunk in enumerate(chunks):
        story.extend(page_hdr(notification_no))
        hdr_r  = header_rows()
        data_r = [data_row(pidx*students_per_page+i+1, st) for i,st in enumerate(chunk)]
        all_r  = hdr_r+data_r
        tbl=Table(all_r,colWidths=col_w,repeatRows=4)
        tbl.setStyle(TableStyle(tbl_styles(4,len(data_r))))
        story.append(tbl)
        story.append(Spacer(1,8))
        story.append(HRFlowable(width="100%",thickness=0.5,color=GRAY,spaceAfter=4))
        story.append(footer_block())
        story.append(Spacer(1,4))
        story.append(P(
            "<i>This result is issued subject to the rectification of any error or "
            "omission as and when detected.</i>",
            S("fn",fontSize=6.5,fontName="Helvetica-Oblique",alignment=TA_CENTER,textColor=GRAY)))
        if pidx<len(chunks)-1:
            story.append(PageBreak())

    doc.build(story)
    print(f"✓  Class sheet → {output_path}")


# ═══════════════════════════════════════════════════════════════════════════════
#  DEMO
# ═══════════════════════════════════════════════════════════════════════════════
if __name__=="__main__":
    os.makedirs("/mnt/user-data/outputs",exist_ok=True)

    # ── Student card ──────────────────────────────────────────────────────────
    SALEH=[
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
    sgpa=calc_sgpa(SALEH)
    print(f"SGPA={sgpa:.9f} → {round(sgpa,2):.2f}  (expected 3.51 ✓)")
    prev=[3.05,3.20,3.19]
    cgpa=calc_cgpa(prev+[sgpa])
    print(f"CGPA={cgpa:.9f} → {round(cgpa,2):.2f}")

    generate_student_result_card(
        output_path   ="/mnt/user-data/outputs/StudentResult_MuhammadSalehHayat.pdf",
        parent_name   ="Gul Nisar",
        student_name  ="Muhammad Saleh Hayat",
        reg_no        ="232201070",
        class_name    ="BCS-9",
        semester_label="4th Semester",
        result_title  ="Result – Spring – 2025",
        subjects      =SALEH,
        previous_sgpas=prev,
        current_pos=14, overall_pos=23, total_students=83,
    )

    # ── Class result sheet ─────────────────────────────────────────────────────
    SUBJS=[
        {"code":"OS301", "name":"Operating Systems (Theory)",         "credit_hours":3,"type":"Theory"},
        {"code":"OS302", "name":"Operating Systems (Lab)",            "credit_hours":1,"type":"Lab"},
        {"code":"CG301", "name":"HCI & Computer Graphics (Theory)",   "credit_hours":3,"type":"Theory"},
        {"code":"CG302", "name":"HCI & Computer Graphics (Practical)","credit_hours":1,"type":"Lab"},
        {"code":"CA301", "name":"Computer Architecture (Theory)",     "credit_hours":2,"type":"Theory"},
        {"code":"CA302", "name":"Computer Architecture (Practical)",  "credit_hours":1,"type":"Lab"},
        {"code":"WT301", "name":"Web Technologies (Theory)",          "credit_hours":3,"type":"Theory"},
        {"code":"WT302", "name":"Web Technologies (Practical)",       "credit_hours":1,"type":"Lab"},
        {"code":"MA301", "name":"Mobile Application (Theory)",        "credit_hours":3,"type":"Theory"},
        {"code":"MA302", "name":"Mobile Application (Practical)",     "credit_hours":1,"type":"Lab"},
        {"code":"IM301", "name":"Introduction to Management",         "credit_hours":3,"type":"Theory"},
    ]

    # Students matching the real image reference
    STUDS=[
        {"name":"Muhammad Siraj Khan",     "reg_no":"23220105",  "father_name":"Muhammad Iqbal",
         "grades":{"OS301":"A","OS302":"B","CG301":"A","CG302":"A","CA301":"A","CA302":"A","WT301":"A-","WT302":"A","MA301":"B","MA302":"B+","IM301":"A"},
         "previous_sgpas":[3.25,3.40,3.50,3.60]},
        {"name":"Muhammad Asif Kayani",    "reg_no":"23220106",  "father_name":"Kayani Sb",
         "grades":{"OS301":"C","OS302":"A","CG301":"A","CG302":"B+","CA301":"B-","CA302":"C","WT301":"A-","WT302":"A","MA301":"B","MA302":"B+","IM301":"A-"},
         "previous_sgpas":[2.90,3.00,3.10,3.20]},
        {"name":"Nooran Ahmed",            "reg_no":"23220155",  "father_name":"Ahmed Khan",
         "grades":{"OS301":"C","OS302":"A","CG301":"B+","CG302":"A","CA301":"B","CA302":"C","WT301":"A","WT302":"A","MA301":"C","MA302":"B-","IM301":"B"},
         "previous_sgpas":[2.80,2.95,3.00,3.05]},
        {"name":"Muhammad Asad Iqbal",     "reg_no":"23220164",  "father_name":"Iqbal Sb",
         "grades":{"OS301":"C","OS302":"A","CG301":"B-","CG302":"B+","CA301":"B","CA302":"C","WT301":"B+","WT302":"A","MA301":"C","MA302":"B","IM301":"B"},
         "previous_sgpas":[2.70,2.80,2.90,2.95]},
        {"name":"Qaiser Ahmed",            "reg_no":"23220155",  "father_name":"Ahmed Sb",
         "grades":{"OS301":"C","OS302":"A","CG301":"C+","CG302":"B+","CA301":"B","CA302":"C","WT301":"A-","WT302":"A","MA301":"C","MA302":"B-","IM301":"B"},
         "previous_sgpas":[2.50,2.60,2.70,2.80]},
        {"name":"Amna Ijaz",               "reg_no":"23220165",  "father_name":"Ijaz Ahmed",
         "grades":{"OS301":"C","OS302":"A","CG301":"A","CG302":"A","CA301":"C","CA302":"C","WT301":"B-","WT302":"A","MA301":"B+","MA302":"A","IM301":"A"},
         "previous_sgpas":[2.80,2.90,3.00,3.10]},
        {"name":"Muhammad Munir",          "reg_no":"23220155",  "father_name":"Munir Sb",
         "grades":{"OS301":"C","OS302":"B+","CG301":"B","CG302":"A","CA301":"B","CA302":"C","WT301":"B","WT302":"A","MA301":"C","MA302":"C","IM301":"B"},
         "previous_sgpas":[2.50,2.55,2.60,2.65]},
        {"name":"Sanzari Fatima",          "reg_no":"23220164",  "father_name":"Fatima Sb",
         "grades":{"OS301":"B","OS302":"A","CG301":"C","CG302":"B","CA301":"C","CA302":"C","WT301":"A","WT302":"A","MA301":"B-","MA302":"B","IM301":"B+"},
         "previous_sgpas":[2.60,2.70,2.75,2.80]},
        {"name":"Muhammad Farooq",         "reg_no":"21220164",  "father_name":"Farooq Sb",
         "grades":{"OS301":"A","OS302":"A","CG301":"B+","CG302":"A","CA301":"A-","CA302":"C","WT301":"A","WT302":"A","MA301":"A","MA302":"A","IM301":"A"},
         "previous_sgpas":[3.20,3.30,3.40,3.45]},
        {"name":"Muhammad Ammar Hashmi",   "reg_no":"23220164",  "father_name":"Hashmi Sb",
         "grades":{"OS301":"B","OS302":"A","CG301":"C","CG302":"A","CA301":"B","CA302":"C","WT301":"A","WT302":"A","MA301":"B","MA302":"B","IM301":"B"},
         "previous_sgpas":[2.70,2.75,2.80,2.85]},
        {"name":"Khadija Tul Kubra",       "reg_no":"23220167",  "father_name":"Kubra Sb",
         "grades":{"OS301":"A-","OS302":"A","CG301":"A","CG302":"A","CA301":"B-","CA302":"C","WT301":"A","WT302":"A","MA301":"A","MA302":"A","IM301":"A"},
         "previous_sgpas":[3.30,3.40,3.45,3.50]},
        {"name":"Muhammad Iftaq",          "reg_no":"23220168",  "father_name":"Iftaq Sb",
         "grades":{"OS301":"C","OS302":"A","CG301":"C","CG302":"B","CA301":"C","CA302":"C","WT301":"B","WT302":"B","MA301":"C","MA302":"B-","IM301":"B"},
         "previous_sgpas":[2.20,2.30,2.40,2.45]},
        {"name":"Muhammad Saleh Hayat",    "reg_no":"232201070", "father_name":"Gul Nisar",
         "grades":{"OS301":"A","OS302":"A","CG301":"B+","CG302":"A","CA301":"B","CA302":"C","WT301":"A","WT302":"A","MA301":"B+","MA302":"A","IM301":"A"},
         "previous_sgpas":[2.80,2.90,3.00,3.10]},
        {"name":"Muhammad Saleh Hayat 2",  "reg_no":"23220170",  "father_name":"Gul Nisar",
         "grades":{"OS301":"A","OS302":"A","CG301":"A","CG302":"A","CA301":"A","CA302":"A","WT301":"A","WT302":"A","MA301":"A","MA302":"A","IM301":"A"},
         "previous_sgpas":[3.40,3.50,3.60,3.70]},
        {"name":"Um-e-Nayab",              "reg_no":"23220171",  "father_name":"Nayab Father",
         "grades":{"OS301":"B","OS302":"A","CG301":"B","CG302":"A","CA301":"C","CA302":"C","WT301":"B","WT302":"B+","MA301":"B","MA302":"B","IM301":"B+"},
         "previous_sgpas":[2.80,2.90,3.00,3.10]},
        {"name":"Muhammad Farooq 2",       "reg_no":"21220172",  "father_name":"Farooq Father",
         "grades":{"OS301":"D","OS302":"B","CG301":"B","CG302":"B","CA301":"C","CA302":"C","WT301":"C","WT302":"C","MA301":"C","MA302":"C","IM301":"C"},
         "previous_sgpas":[1.70,1.80,1.85,1.90]},
        {"name":"Zaigham Shahid Raja",     "reg_no":"23220107",  "father_name":"Shahid Sb",
         "grades":{"OS301":"C","OS302":"B","CG301":"B-","CG302":"B","CA301":"C","CA302":"C","WT301":"B","WT302":"B","MA301":"C","MA302":"C","IM301":"B"},
         "previous_sgpas":[2.10,2.20,2.30,2.35]},
        {"name":"Gull Fareed",             "reg_no":"23220108",  "father_name":"Fareed Sb",
         "grades":{"OS301":"B","OS302":"B+","CG301":"B","CG302":"B+","CA301":"C","CA302":"C","WT301":"A","WT302":"A","MA301":"B","MA302":"B","IM301":"A"},
         "previous_sgpas":[2.60,2.70,2.80,2.85]},
        {"name":"Muhammad Rafique",        "reg_no":"23220109",  "father_name":"Rafique Sb",
         "grades":{"OS301":"C","OS302":"B","CG301":"B","CG302":"A","CA301":"C","CA302":"C","WT301":"B","WT302":"B+","MA301":"C","MA302":"B","IM301":"B"},
         "previous_sgpas":[2.40,2.50,2.55,2.60]},
        {"name":"Abdul Rauf",              "reg_no":"23220110",  "father_name":"Rauf Sb",
         "grades":{"OS301":"B","OS302":"A","CG301":"A","CG302":"A","CA301":"B","CA302":"C","WT301":"A","WT302":"A","MA301":"B+","MA302":"A","IM301":"A"},
         "previous_sgpas":[3.00,3.10,3.15,3.20]},
        {"name":"Abid ran",                "reg_no":"23220111",  "father_name":"Ran Father",
         "grades":{"OS301":"C","OS302":"B","CG301":"C","CG302":"B","CA301":"C","CA302":"C","WT301":"B","WT302":"B","MA301":"C","MA302":"C","IM301":"B"},
         "previous_sgpas":[2.00,2.10,2.15,2.20]},
        {"name":"Gull Nisar",              "reg_no":"23220112",  "father_name":"Nisar Father",
         "grades":{"OS301":"A","OS302":"A","CG301":"A","CG302":"A","CA301":"A","CA302":"B+","WT301":"A","WT302":"A","MA301":"A","MA302":"A","IM301":"A"},
         "previous_sgpas":[3.50,3.60,3.65,3.70]},
        {"name":"Shahid Salem Raju",       "reg_no":"23220113",  "father_name":"Salem Sb",
         "grades":{"OS301":"B","OS302":"A","CG301":"B","CG302":"A","CA301":"C","CA302":"C","WT301":"B","WT302":"A","MA301":"B","MA302":"B","IM301":"B"},
         "previous_sgpas":[2.60,2.70,2.75,2.80]},
        {"name":"Raza Nizammudin Abbas",   "reg_no":"23220114",  "father_name":"Nizammudin",
         "grades":{"OS301":"B","OS302":"A","CG301":"C+","CG302":"B+","CA301":"C","CA302":"C","WT301":"B","WT302":"A","MA301":"B","MA302":"B-","IM301":"B"},
         "previous_sgpas":[2.40,2.50,2.55,2.60]},
        {"name":"Umm Ali Naeem",           "reg_no":"23220115",  "father_name":"Naeem Sb",
         "grades":{"OS301":"C","OS302":"A","CG301":"C","CG302":"B","CA301":"D+","CA302":"C","WT301":"B","WT302":"A","MA301":"C","MA302":"C","IM301":"B"},
         "previous_sgpas":[1.90,2.00,2.05,2.10]},
        {"name":"Mukhtar Ahmed",           "reg_no":"23220116",  "father_name":"Ahmed Sb",
         "grades":{"OS301":"B","OS302":"B+","CG301":"B","CG302":"B","CA301":"C","CA302":"C","WT301":"B","WT302":"B","MA301":"B","MA302":"B","IM301":"B"},
         "previous_sgpas":[2.50,2.60,2.65,2.70]},
        {"name":"Abdur Masood",            "reg_no":"23220117",  "father_name":"Masood Sb",
         "grades":{"OS301":"B+","OS302":"A","CG301":"B+","CG302":"A","CA301":"B","CA302":"C","WT301":"A","WT302":"A","MA301":"B+","MA302":"A","IM301":"A"},
         "previous_sgpas":[3.00,3.10,3.15,3.20]},
        {"name":"Abdul Maecd",             "reg_no":"23220118",  "father_name":"Maecd Father",
         "grades":{"OS301":"B","OS302":"A","CG301":"D+","CG302":"B","CA301":"C","CA302":"C","WT301":"B-","WT302":"A","MA301":"C","MA302":"B","IM301":"B"},
         "previous_sgpas":[2.20,2.30,2.35,2.40]},
        {"name":"Shahyar Ahmed Kiani",     "reg_no":"23220119",  "father_name":"Kiani Sb",
         "grades":{"OS301":"A","OS302":"A","CG301":"A","CG302":"A","CA301":"B+","CA302":"C","WT301":"A","WT302":"A","MA301":"A","MA302":"A","IM301":"A"},
         "previous_sgpas":[3.40,3.50,3.55,3.60]},
        {"name":"Nayab Gul",               "reg_no":"23220120",  "father_name":"Gul Father",
         "grades":{"OS301":"A","OS302":"A","CG301":"B+","CG302":"A","CA301":"A","CA302":"A","WT301":"A","WT302":"A","MA301":"A","MA302":"A","IM301":"A"},
         "previous_sgpas":[3.70,3.75,3.80,3.82]},
        {"name":"Irfan Atam",              "reg_no":"23220121",  "father_name":"Atam Sb",
         "grades":{"OS301":"B","OS302":"B","CG301":"B","CG302":"B","CA301":"C","CA302":"C","WT301":"B","WT302":"B","MA301":"B","MA302":"B","IM301":"B"},
         "previous_sgpas":[2.50,2.55,2.60,2.65]},
        {"name":"Syed Shahzad Ather",      "reg_no":"23220122",  "father_name":"Ather Sb",
         "grades":{"OS301":"B+","OS302":"A","CG301":"B+","CG302":"A","CA301":"B","CA302":"C","WT301":"A","WT302":"A","MA301":"B+","MA302":"A","IM301":"A"},
         "previous_sgpas":[2.90,3.00,3.05,3.10]},
        {"name":"Abdul Rasheed",           "reg_no":"23220123",  "father_name":"Rasheed Sb",
         "grades":{"OS301":"B","OS302":"A","CG301":"B","CG302":"A","CA301":"C","CA302":"C","WT301":"B","WT302":"B+","MA301":"B","MA302":"B","IM301":"B"},
         "previous_sgpas":[2.60,2.70,2.75,2.80]},
        {"name":"Abdul Maeed",             "reg_no":"23220124",  "father_name":"Maeed Father",
         "grades":{"OS301":"B-","OS302":"B","CG301":"A","CG302":"A","CA301":"C","CA302":"C","WT301":"B+","WT302":"A","MA301":"A","MA302":"A","IM301":"A"},
         "previous_sgpas":[2.80,2.90,2.95,3.00]},
        {"name":"Nayyab Gul 2",            "reg_no":"23220125",  "father_name":"Gul 2 Father",
         "grades":{"OS301":"A","OS302":"A","CG301":"A","CG302":"A","CA301":"A","CA302":"B","WT301":"A","WT302":"A","MA301":"A","MA302":"A","IM301":"A"},
         "previous_sgpas":[3.60,3.70,3.75,3.80]},
        {"name":"Syed Ali Murtajiz Bukhari","reg_no":"232201086","father_name":"Murtajiz Bukhari",
         "grades":{"OS301":"B","OS302":"B+","CG301":"B","CG302":"B+","CA301":"C","CA302":"C","WT301":"B+","WT302":"A","MA301":"B","MA302":"B+","IM301":"B+"},
         "previous_sgpas":[2.50,2.60,2.70,2.80]},
        {"name":"Muhammad Waleed Hassan",  "reg_no":"23220127",  "father_name":"Hassan Sb",
         "grades":{"OS301":"C","OS302":"B","CG301":"C","CG302":"B","CA301":"C","CA302":"C","WT301":"C","WT302":"B","MA301":"C","MA302":"C","IM301":"C"},
         "previous_sgpas":[1.90,2.00,2.05,2.10]},
        {"name":"Zubaria Latif",           "reg_no":"23220128",  "father_name":"Latif Sb",
         "grades":{"OS301":"B","OS302":"A","CG301":"B+","CG302":"A","CA301":"C","CA302":"C","WT301":"B+","WT302":"A","MA301":"B+","MA302":"A","IM301":"A"},
         "previous_sgpas":[2.80,2.90,2.95,3.00]},
        {"name":"Maira Bibi",              "reg_no":"23220129",  "father_name":"Bibi Father",
         "grades":{"OS301":"A","OS302":"A-","CG301":"A","CG302":"A","CA301":"B","CA302":"C","WT301":"A","WT302":"A","MA301":"A","MA302":"A","IM301":"A"},
         "previous_sgpas":[3.40,3.45,3.50,3.55]},
        {"name":"Muhammad Asim Wakeel",    "reg_no":"23220130",  "father_name":"Wakeel Sb",
         "grades":{"OS301":"F","OS302":"W/C","CG301":"B-","CG302":"B","CA301":"D+","CA302":"C","WT301":"C","WT302":"C","MA301":"C","MA302":"C","IM301":"C"},
         "previous_sgpas":[1.20,1.30,1.35,1.40]},
        {"name":"Muhammad Asad Afzal",     "reg_no":"23220131",  "father_name":"Afzal Sb",
         "grades":{"OS301":"D+","OS302":"B+","CG301":"B","CG302":"B","CA301":"C","CA302":"C","WT301":"C","WT302":"B","MA301":"C","MA302":"C","IM301":"B"},
         "previous_sgpas":[1.70,1.80,1.85,1.90]},
        {"name":"Muhammad Latif",          "reg_no":"23220132",  "father_name":"Latif Father",
         "grades":{"OS301":"B","OS302":"A","CG301":"B","CG302":"A","CA301":"C","CA302":"C","WT301":"B","WT302":"A","MA301":"B","MA302":"B+","IM301":"B"},
         "previous_sgpas":[2.50,2.60,2.65,2.70]},
        {"name":"Muhammad Faisal",         "reg_no":"23220133",  "father_name":"Faisal Father",
         "grades":{"OS301":"C","OS302":"B","CG301":"C","CG302":"B","CA301":"C","CA302":"C","WT301":"B","WT302":"B","MA301":"C","MA302":"C","IM301":"B"},
         "previous_sgpas":[2.10,2.20,2.25,2.30]},
        {"name":"Muhammad Abdullah Khan",  "reg_no":"23220134",  "father_name":"Abdullah Father",
         "grades":{"OS301":"C","OS302":"B","CG301":"B","CG302":"B","CA301":"C","CA302":"C","WT301":"B","WT302":"B","MA301":"B","MA302":"B","IM301":"B"},
         "previous_sgpas":[2.30,2.40,2.45,2.50]},
        {"name":"Muhammad Usama Bilal",    "reg_no":"23220135",  "father_name":"Bilal Father",
         "grades":{"OS301":"C","OS302":"B","CG301":"B","CG302":"B","CA301":"C","CA302":"C","WT301":"B","WT302":"B","MA301":"C","MA302":"B","IM301":"B"},
         "previous_sgpas":[2.20,2.30,2.35,2.40]},
        {"name":"Talal Maqsam",            "reg_no":"23220136",  "father_name":"Maqsam Father",
         "grades":{"OS301":"B","OS302":"A","CG301":"B","CG302":"A","CA301":"C","CA302":"C","WT301":"B","WT302":"B+","MA301":"B","MA302":"B","IM301":"B"},
         "previous_sgpas":[2.50,2.60,2.65,2.70]},
        {"name":"Abdal Maqam",             "reg_no":"23220137",  "father_name":"Maqam Father",
         "grades":{"OS301":"C","OS302":"B","CG301":"C","CG302":"B","CA301":"C","CA302":"C","WT301":"C","WT302":"B","MA301":"C","MA302":"C","IM301":"C"},
         "previous_sgpas":[1.80,1.90,1.95,2.00]},
        {"name":"Shafiq Hussain",          "reg_no":"23220138",  "father_name":"Hussain Father",
         "grades":{"OS301":"B","OS302":"A","CG301":"B","CG302":"A","CA301":"C","CA302":"C","WT301":"B","WT302":"A","MA301":"B","MA302":"B","IM301":"B"},
         "previous_sgpas":[2.60,2.70,2.75,2.80]},
        {"name":"Muhammad Jamil",          "reg_no":"23220139",  "father_name":"Jamil Father",
         "grades":{"OS301":"B","OS302":"A","CG301":"B","CG302":"A","CA301":"C","CA302":"C","WT301":"B","WT302":"A","MA301":"B","MA302":"B","IM301":"B"},
         "previous_sgpas":[2.70,2.75,2.80,2.85]},
        {"name":"Arasha Jahangeer",        "reg_no":"32220169",  "father_name":"Jahangeer Sb",
         "grades":{"OS301":"B+","OS302":"A","CG301":"A","CG302":"A","CA301":"B","CA302":"A","WT301":"A","WT302":"A","MA301":"A","MA302":"A","IM301":"A"},
         "previous_sgpas":[3.30,3.40,3.45,3.50]},
        {"name":"Laraib Faiz",             "reg_no":"32220107",  "father_name":"Faiz Sb",
         "grades":{"OS301":"C","OS302":"A","CG301":"B","CG302":"B+","CA301":"C","CA302":"C","WT301":"B","WT302":"B","MA301":"C","MA302":"B","IM301":"B"},
         "previous_sgpas":[2.40,2.50,2.55,2.60]},
        {"name":"Muhammad Bilal",          "reg_no":"32220109",  "father_name":"Bilal Father",
         "grades":{"OS301":"B","OS302":"A","CG301":"B","CG302":"A","CA301":"C","CA302":"C","WT301":"B","WT302":"B+","MA301":"B","MA302":"B","IM301":"B"},
         "previous_sgpas":[2.60,2.65,2.70,2.75]},
        {"name":"Muhammad Nadir",          "reg_no":"33220100",  "father_name":"Nadir Father",
         "grades":{"OS301":"C","OS302":"B","CG301":"C","CG302":"B","CA301":"C","CA302":"C","WT301":"C","WT302":"B","MA301":"C","MA302":"C","IM301":"C"},
         "previous_sgpas":[1.80,1.85,1.90,1.95]},
    ]

    generate_class_result_sheet(
        output_path      ="/mnt/user-data/outputs/ClassResult_BCS9_Semester6.pdf",
        batch_name       ="BCS-9",
        semester_no      =6,
        semester_label   ="Spring 2026",
        program          ="BS Computer Science",
        subjects         =SUBJS,
        students         =STUDS,
        students_per_page=50,
        notification_no  ="IST/Exams/KICSIT/BCS/6/#3025/03  dated 11-02-2026",
        je_name          ="Muhammad Latif",
        je_title         ="JE (Exams) KICSIT",
        examiner_name    ="Faheem Ahmed",
        examiner_title   ="Deputy Controller of Examinations KICSIT",
    )
