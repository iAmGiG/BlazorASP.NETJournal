"""
Build C-Day Computing Showcase poster for GexVisor.
Uses the official KSU c-day-template4.pptx as base, preserving branding.
"""

from pptx import Presentation
from pptx.util import Inches, Pt, Emu
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN, MSO_ANCHOR
from pptx.enum.shapes import MSO_SHAPE
import os
import copy

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
TEMPLATE = os.path.join(SCRIPT_DIR, "c-day-template4.pptx")
OUTPUT = os.path.join(SCRIPT_DIR, "GexVisor_CDay_Poster.pptx")

# ── Colors (matched from template) ────────────────────────────────────
KSU_GOLD = RGBColor(0xFF, 0xC0, 0x00)  # Template's gold from Author text
WHITE = RGBColor(0xFF, 0xFF, 0xFF)
BLACK = RGBColor(0x00, 0x00, 0x00)
DARK_BG = RGBColor(0x2B, 0x2B, 0x2B)
LIGHT_PLACEHOLDER = RGBColor(0xE8, 0xE8, 0xE8)
MID_GRAY = RGBColor(0x66, 0x66, 0x66)
SECTION_BG = RGBColor(0x3A, 0x3A, 0x3A)

prs = Presentation(TEMPLATE)
slide = prs.slides[0]

# ── Identify shapes to keep vs remove ──────────────────────────────────
# Keep: footer freeform (object 28), KSU logo (Picture 22), footer line (Straight Connector 24)
# Keep: QR code image (Picture 13) — we'll reposition it
# Modify: title placeholder, author text, project number
# Remove: all template placeholder content boxes

KEEP_NAMES = {'object 28', 'Picture 22', 'Straight Connector 24', 'Picture 13'}
KEEP_TITLE = 'object 2'
KEEP_AUTHOR = 'object 39'
KEEP_NUMBER = 'Text Box 19'

shapes_to_remove = []
for shape in slide.shapes:
    if shape.name not in KEEP_NAMES and shape.name != KEEP_TITLE \
       and shape.name != KEEP_AUTHOR and shape.name != KEEP_NUMBER:
        shapes_to_remove.append(shape)

# Remove template placeholder content (but keep branding elements)
for shape in shapes_to_remove:
    sp = shape._element
    sp.getparent().remove(sp)


# ── Update title ───────────────────────────────────────────────────────
for shape in slide.shapes:
    if shape.name == KEEP_TITLE:
        tf = shape.text_frame
        tf.clear()
        p = tf.paragraphs[0]
        run = p.add_run()
        run.text = "GexVisor: A Cross-Platform Framework for Interactive Gamma Exposure Analysis"
        run.font.name = "Arial"
        run.font.size = Pt(110)
        run.font.bold = True
        run.font.color.rgb = WHITE

    elif shape.name == KEEP_NUMBER:
        tf = shape.text_frame
        tf.clear()
        p = tf.paragraphs[0]
        run = p.add_run()
        run.text = "CS-PhD"
        run.font.size = Pt(120)
        run.font.bold = True
        run.font.color.rgb = WHITE

    elif shape.name == KEEP_AUTHOR:
        tf = shape.text_frame
        tf.clear()
        # Author line
        p = tf.paragraphs[0]
        run = p.add_run()
        run.text = "Christopher Regan"
        run.font.name = "Arial"
        run.font.size = Pt(60)
        run.font.bold = True
        run.font.color.rgb = KSU_GOLD
        # Advisor line
        p2 = tf.add_paragraph()
        run2 = p2.add_run()
        run2.text = "Advisor: Dr. Ying Xie"
        run2.font.name = "Arial"
        run2.font.size = Pt(60)
        run2.font.bold = True
        run2.font.color.rgb = KSU_GOLD
        # Department line
        p3 = tf.add_paragraph()
        run3 = p3.add_run()
        run3.text = "College of Computing and Software Engineering"
        run3.font.name = "Arial"
        run3.font.size = Pt(44)
        run3.font.bold = False
        run3.font.color.rgb = WHITE


# ── Helper functions ───────────────────────────────────────────────────
def add_rect(left, top, width, height, fill_color=None, line_color=None, line_width=Pt(0)):
    shape = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, left, top, width, height)
    shape.shadow.inherit = False
    if fill_color:
        shape.fill.solid()
        shape.fill.fore_color.rgb = fill_color
    else:
        shape.fill.background()
    if line_color:
        shape.line.color.rgb = line_color
        shape.line.width = line_width
    else:
        shape.line.fill.background()
    return shape


def add_rounded_rect(left, top, width, height, fill_color=None, line_color=None, line_width=Pt(0)):
    shape = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, left, top, width, height)
    shape.shadow.inherit = False
    if fill_color:
        shape.fill.solid()
        shape.fill.fore_color.rgb = fill_color
    else:
        shape.fill.background()
    if line_color:
        shape.line.color.rgb = line_color
        shape.line.width = line_width
    else:
        shape.line.fill.background()
    return shape


def add_text_box(left, top, width, height, text, font_size=Pt(40),
                 font_color=BLACK, bold=False, alignment=PP_ALIGN.LEFT,
                 font_name="Arial"):
    txBox = slide.shapes.add_textbox(left, top, width, height)
    txBox.text_frame.word_wrap = True
    txBox.text_frame.auto_size = None
    p = txBox.text_frame.paragraphs[0]
    run = p.add_run()
    run.text = text
    run.font.size = font_size
    run.font.color.rgb = font_color
    run.font.bold = bold
    run.font.name = font_name
    p.alignment = alignment
    return txBox


def add_image_placeholder(left, top, width, height, label, sublabel=""):
    """Dashed-border box with centered label for screenshot drop-in."""
    shape = add_rounded_rect(left, top, width, height,
                             fill_color=LIGHT_PLACEHOLDER,
                             line_color=MID_GRAY, line_width=Pt(2))
    shape.line.dash_style = 4  # Dash

    add_text_box(left, top + height // 2 - Inches(0.45), width, Inches(0.6),
                 label, font_size=Pt(36), font_color=MID_GRAY,
                 bold=True, alignment=PP_ALIGN.CENTER)
    if sublabel:
        add_text_box(left, top + height // 2 + Inches(0.2), width, Inches(0.5),
                     sublabel, font_size=Pt(22), font_color=MID_GRAY,
                     alignment=PP_ALIGN.CENTER)
    return shape


def add_section_header(left, top, width, text):
    """Gold accent bar + white section title on dark card."""
    add_rect(left, top, Inches(0.2), Inches(0.7), fill_color=KSU_GOLD)
    add_text_box(left + Inches(0.4), top - Inches(0.02), width - Inches(0.4), Inches(0.7),
                 text, font_size=Pt(48), font_color=WHITE,
                 bold=True, font_name="Arial")
    return top + Inches(0.85)


def add_bullet_text(left, top, width, items, font_size=Pt(32), font_color=WHITE):
    """Add a text box with bullet points."""
    txBox = slide.shapes.add_textbox(left, top, width, Inches(len(items) * 0.6 + 0.1))
    txBox.text_frame.word_wrap = True
    for i, item in enumerate(items):
        if i == 0:
            p = txBox.text_frame.paragraphs[0]
        else:
            p = txBox.text_frame.add_paragraph()
        run = p.add_run()
        run.text = item
        run.font.size = font_size
        run.font.color.rgb = font_color
        run.font.name = "Arial"
        p.space_after = Pt(6)
    return txBox


# ═══════════════════════════════════════════════════════════════════════
# CONTENT AREA — positioned within the template's large right rectangle
# Template has: left column ~1.67-13.75 in, right area ~14.75-48 in
# Footer starts at y=33.0
# Title area: y=0 to ~3.0
# ═══════════════════════════════════════════════════════════════════════

# -- Dark background card for right content area (replaces Rectangle 10)
add_rect(Inches(14.75), Inches(3.0), Inches(33.25), Inches(30.0),
         fill_color=DARK_BG)

# ── LEFT COLUMN (x=1.5 to 13.5) ──────────────────────────────────────
LEFT_X = Inches(1.5)
LEFT_W = Inches(12.0)
y = Inches(3.5)

# Subtitle / engineering angle
add_text_box(LEFT_X, y, LEFT_W, Inches(2.0),
             "Architecture, WebAssembly, and Test Infrastructure "
             "for a .NET Financial Visualization Platform",
             font_size=Pt(44), font_color=KSU_GOLD, bold=True,
             alignment=PP_ALIGN.LEFT)
y += Inches(2.5)

# The Problem
y = add_section_header(LEFT_X, y, LEFT_W, "The Problem")
add_bullet_text(LEFT_X + Inches(0.2), y, LEFT_W - Inches(0.4), [
    "\u2022 Existing GEX tools: fragmented JS scripts, no tests, no CI",
    "\u2022 Options visualization needs real-time + historical views",
    "\u2022 Research workflows disconnected from analysis tooling",
    "\u2022 No cross-platform solution (browser-based needed)",
], font_size=Pt(34))
y += Inches(3.2)

# Architecture
y = add_section_header(LEFT_X, y, LEFT_W, "System Architecture")
add_image_placeholder(LEFT_X, y, LEFT_W, Inches(6.0),
                      "ARCHITECTURE DIAGRAM",
                      "3-tier: UI \u2192 Core \u2192 API")
y += Inches(6.3)

# Tech stack
y = add_section_header(LEFT_X, y, LEFT_W, "Technology Stack")
add_bullet_text(LEFT_X + Inches(0.2), y, LEFT_W - Inches(0.4), [
    "\u2022 .NET 10 / Blazor WebAssembly (runs in browser via WASM)",
    "\u2022 C# throughout \u2014 replaced JS/Python prototype entirely",
    "\u2022 ApexCharts for interactive financial visualizations",
    "\u2022 SQLite caching layer for market data persistence",
    "\u2022 ASP.NET Core API with options chain services",
    "\u2022 GitHub Actions CI/CD + Husky.Net pre-commit hooks",
], font_size=Pt(32))

# ── RIGHT AREA — split into 2 columns of screenshots ─────────────────
# Right area spans 14.75 to ~47.25 (with margin)
R_LEFT = Inches(15.5)
R_WIDTH = Inches(31.0)
COL_GAP = Inches(0.6)
COL_W = (R_WIDTH - COL_GAP) / 2
col_a = R_LEFT
col_b = R_LEFT + COL_W + COL_GAP

# ─── Right Column A ──────────────────────────────────────────────────
y = Inches(3.5)

y = add_section_header(col_a, y, COL_W, "GEX Visualizer")
add_image_placeholder(col_a, y, COL_W, Inches(6.5),
                      "GEX VISUALIZER",
                      "Interactive chart + regime timeline + annotations")
y += Inches(6.9)

y = add_section_header(col_a, y, COL_W, "Research Notebook")
add_image_placeholder(col_a, y, COL_W, Inches(6.0),
                      "RESEARCH NOTEBOOK",
                      "KDD process tracking + complexity radar")
y += Inches(6.4)

y = add_section_header(col_a, y, COL_W, "Paper Trading Journal")
add_image_placeholder(col_a, y, COL_W, Inches(6.0),
                      "PAPER TRADING",
                      "Trade grid + decision timeline + regime context")
y += Inches(6.4)

# Stats caption
add_text_box(col_a, y, COL_W, Inches(0.6),
             "15 pages  \u2022  35+ Blazor components  \u2022  Full keyboard navigation",
             font_size=Pt(28), font_color=KSU_GOLD,
             alignment=PP_ALIGN.CENTER)

# ─── Right Column B ──────────────────────────────────────────────────
y = Inches(3.5)

y = add_section_header(col_b, y, COL_W, "Research Arcade")
add_image_placeholder(col_b, y, COL_W, Inches(5.5),
                      "RESEARCH ARCADE",
                      "Pattern discovery + data pipeline state")
y += Inches(5.9)

y = add_section_header(col_b, y, COL_W, "Task Board & CI/CD")
# Two side-by-side
half_w = (COL_W - Inches(0.4)) / 2
add_image_placeholder(col_b, y, half_w, Inches(5.5),
                      "TASK BOARD", "GitHub Kanban")
add_image_placeholder(col_b + half_w + Inches(0.4), y, half_w, Inches(5.5),
                      "CI/CD PIPELINE", "GitHub Actions")
y += Inches(5.9)

y = add_section_header(col_b, y, COL_W, "Test Infrastructure")
add_image_placeholder(col_b, y, half_w, Inches(5.0),
                      "TEST RESULTS", "xUnit + Moq + Coverlet")
add_image_placeholder(col_b + half_w + Inches(0.4), y, half_w, Inches(5.0),
                      "BACKTEST TRACKER", "Strategy validation")
y += Inches(5.4)

# Engineering decisions
y = add_section_header(col_b, y, COL_W, "Key Engineering Decisions")
add_bullet_text(col_b + Inches(0.2), y, COL_W - Inches(0.4), [
    "\u2022 JS/Python \u2192 .NET: type safety, single-language stack",
    "\u2022 WebAssembly: full app runs client-side in browser",
    "\u2022 3-tier separation: UI / Core / API testable independently",
    "\u2022 Load testing suite for API performance validation",
    "\u2022 StyleCop + EditorConfig for consistent code quality",
], font_size=Pt(30))

# ── QR Code placeholder (above footer, right side) ────────────────────
add_image_placeholder(Inches(43.0), Inches(27.5), Inches(3.5), Inches(3.5),
                      "QR CODE", "Link to repo/demo")

# ── Save ───────────────────────────────────────────────────────────────
prs.save(OUTPUT)
print(f"Poster saved to: {OUTPUT}")
