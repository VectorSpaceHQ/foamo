#!/usr/bin/env python3
# Convert png silhouette into gcode
import cv2
import numpy as np
import os
import sys
import math
import re

def scale_gcode(infile):
    max_x = 800
    max_y = 800

    with open(infile, "r") as f:
        lines = f.readlines()

    peak_x = 0
    peak_y = 0
    for line in lines:
        x = re.search('X[0-9]+\.[0-9]*', line)
        x = x[1:] # drop the X
        y = re.search('Y[0-9]+\.[0-9]*', line)
        y = y[1:] # drop the Y
        if x > peak_x:
            peak_x = x
        if y > peak_y:
            peak_y = y

    x_scale = max_x / peak_x
    y_scale = max_y / peak_y

    newlines = []
    for line in lines:
        x = re.search('X[0-9]+\.[0-9]*', line)
        x = x[1:] # drop the X
        y = re.search('Y[0-9]+\.[0-9]*', line)
        y = y[1:] # drop the X
        newline = re.sub(x, str(x*x_scale), line)
        newline = re.sub(y, str(y*y_scale), newline)
        newlines.append(newline)

    with open(infile+"-scaled", "w") as f:
        for newline in newlines:
            f.writeline(newline)

def calc_dist(p1, p2):
    x1, y1 = p1
    x2, y2 = p2
    dist = math.sqrt((x2-x1)**2 + (y2-y1)**2)
    return dist

def nearest_neighbor(points):
    sorted_points = [[100,5]]

    target_len = len(points)
    while len(sorted_points) < target_len:
        current_point = sorted_points[-1]
        min_d = 10E12
        min_idx = 10E9
        for i in range(len(points)):
            d = calc_dist(current_point, points[i])
            if d < min_d:
                min_d = d
                min_idx = i
        if min_d > 50:
            print("long line needs to be dropped", min_idx, min_d, len(points))
            target_len -= 1
        else:
            sorted_points.append(points[min_idx])
        points.pop(min_idx)
    return sorted_points


def points_to_gcode(points, filename):
    basefile = os.path.basename(filename)
    outname = os.path.splitext(basefile)[0]
    with open(outname + '.nc', "w+") as f:
        f.write('(new profile)\n')
        f.write('G21\n') # mm
        f.write('G28XY\n') # mm
        for point in points:
            x, y = point
            speed = 800 # feedrate (mm/min)
            f.write("G1 X{} Y{} F{}\n".format(x,y, speed))


if __name__ == "__main__":
    try:
        infile = sys.argv[1]
    except Exception as e:
        print("Error: Input file not provided")
        print("Usage: $ ./silhouette2gcode.py infile.png")
        sys.exit()

    im = cv2.imread(infile)
    im_flipped = cv2.flip(im, 0)
    imgray = cv2.cvtColor(im, cv2.COLOR_BGR2GRAY)
    # ret, thresh = cv2.threshold(imgray, 127, 255, 0)
    ret, thresh = cv2.threshold(imgray, 0, 255, 0, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)
        # ret3, thresh = cv.threshold(blur, 0, 255, cv.THRESH_BINARY_INV + cv.THRESH_OTSU)
    contours, hierarchy = cv2.findContours(thresh, cv2.RETR_TREE, cv2.CHAIN_APPROX_NONE)
    # contours, hierarchy = cv2.findContours(thresh, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)

    cont = cv2.drawContours(im, contours, -1, (0,255,0), 3)
    points = []
    for c in contours:
        for point in c:
            points.append(point[0])

    sorted_points = nearest_neighbor(points)
    points_to_gcode(sorted_points, infile)
    # points_to_gcode(points, infile)

    # cv2.imshow('Edges in the image', cont) #displaying the image
    # cv2.waitKey(0)
