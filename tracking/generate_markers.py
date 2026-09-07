#!/usr/bin/env python3
"""
ArUco Marker Generator for ZIKJ Karts
Creates printable PDF sheet with all kart markers.

Usage:
    python generate_markers.py --count 6 --size 100 --output markers.pdf
"""

import argparse
import sys
import tempfile
from pathlib import Path

import cv2
import numpy as np


def generate_markers(count: int, marker_size_px: int, output_dir: str):
    """Generate individual marker images."""
    print(f"[GEN] Generating {count} ArUco markers ({marker_size_px}px)...")

    output_dir = Path(output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)
    aruco_dict = cv2.aruco.getPredefinedDictionary(cv2.aruco.DICT_5X5_250)

    for marker_id in range(count):
        # Generate marker image
        marker_image = cv2.aruco.generateImageMarker(
            aruco_dict, id=marker_id, sidePixels=marker_size_px
        )

        # Add white border and ID label
        bordered = cv2.copyMakeBorder(
            marker_image, 50, 50, 50, 50, cv2.BORDER_CONSTANT, value=255
        )

        # Add ID text below marker
        cv2.putText(
            bordered,
            f"Kart {marker_id}",
            (10, marker_size_px + 90),
            cv2.FONT_HERSHEY_SIMPLEX,
            1.5,
            0,
            2,
        )

        # Save
        filename = output_dir / f"marker_{marker_id:02d}.png"
        cv2.imwrite(str(filename), bordered)
        print(f"  [OK] Saved: {filename}")

    print(f"[GEN] All markers generated in {output_dir}/")


def generate_sheet(count: int, marker_size_px: int, output_path: str):
    """Generate a single PDF sheet with all markers."""
    print(f"[GEN] Generating marker sheet ({count} markers)...")

    # Try to use reportlab for PDF (fallback to image sheet)
    try:
        from reportlab.pdfgen import canvas
        from reportlab.lib.pagesizes import A4

        pdf_width, pdf_height = A4
        c = canvas.Canvas(output_path, pagesize=A4)

        aruco_dict = cv2.aruco.getPredefinedDictionary(cv2.aruco.DICT_5X5_250)

        # Layout: 3 columns × 2 rows per page
        cols, rows = 3, 2
        x_start, y_start = 50, pdf_height - 150
        col_width = (pdf_width - 100) / cols
        row_height = (pdf_height - 200) / rows

        marker_idx = 0

        for row in range(rows):
            for col in range(cols):
                if marker_idx >= count:
                    break

                # Generate marker
                marker_image = cv2.aruco.generateImageMarker(
                    aruco_dict, id=marker_idx, sidePixels=marker_size_px
                )

                # Save temp image
                temp_file = Path(tempfile.gettempdir()) / f"zikj_marker_{marker_idx}.png"
                cv2.imwrite(str(temp_file), marker_image)

                # Draw on PDF
                x = x_start + col * col_width
                y = y_start - row * row_height

                # Draw marker (100×100 marker + label)
                c.drawString(x + 10, y - 20, f"Kart {marker_idx} (ID: {marker_idx})")
                c.drawImage(
                    str(temp_file), x + 10, y - marker_size_px - 40, width=100, height=100
                )

                # Print size reference
                c.setFont("Helvetica", 8)
                c.drawString(
                    x + 10, y - marker_size_px - 65, "Print at 100mm × 100mm"
                )

                marker_idx += 1

        c.save()
        print(f"[GEN] PDF sheet saved: {output_path}")

    except ImportError:
        print("[WARN] reportlab not installed, generating PNG sheet instead...")
        # Fallback: create large PNG image
        sheet_width = 2100  # A4 @ 300 DPI
        sheet_height = 2970
        sheet = np.ones((sheet_height, sheet_width, 3), dtype=np.uint8) * 255

        aruco_dict = cv2.aruco.getPredefinedDictionary(cv2.aruco.DICT_5X5_250)

        cols, rows = 3, 2
        x_start, y_start = 100, 100
        col_width = (sheet_width - 200) // cols
        row_height = (sheet_height - 200) // rows

        marker_idx = 0

        for row in range(rows):
            for col in range(cols):
                if marker_idx >= count:
                    break

                marker_image = cv2.aruco.generateImageMarker(
                    aruco_dict, id=marker_idx, sidePixels=400  # Large for printing
                )

                # Paste onto sheet
                x = x_start + col * col_width
                y = y_start + row * row_height

                # Resize marker
                marker_resized = cv2.resize(marker_image, (400, 400))

                # Paste
                sheet[y : y + 400, x : x + 400] = cv2.cvtColor(
                    marker_resized, cv2.COLOR_GRAY2BGR
                )

                # Add label
                cv2.putText(
                    sheet,
                    f"Kart {marker_idx}",
                    (x + 10, y + 430),
                    cv2.FONT_HERSHEY_SIMPLEX,
                    2,
                    (0, 0, 0),
                    3,
                )

                marker_idx += 1

        # Save as PNG
        png_output = output_path.replace(".pdf", ".png")
        cv2.imwrite(png_output, sheet)
        print(f"[GEN] PNG sheet saved: {png_output}")


def main():
    parser = argparse.ArgumentParser(description="Generate ArUco Markers")
    parser.add_argument(
        "--count", type=int, default=6, help="Number of markers to generate"
    )
    parser.add_argument(
        "--size", type=int, default=100, help="Marker size in pixels"
    )
    parser.add_argument(
        "--output",
        type=str,
        default="markers",
        help="Output directory or PDF file",
    )
    parser.add_argument(
        "--sheet", action="store_true", help="Generate single PDF sheet"
    )
    args = parser.parse_args()

    if args.sheet:
        generate_sheet(args.count, args.size, args.output)
    else:
        generate_markers(args.count, args.size, args.output)

    print("[GEN] Done!")


if __name__ == "__main__":
    main()
