#!/usr/bin/env python3
import cv2
import numpy as np
import os


def contour_to_gcode(contours, filename):
    basefile = os.path.basename(filename)
    outname = os.path.splitext(basefile)[0]
    with open(outname + '.nc', "w+") as f:
        f.write('(new profile)\n')
        f.write('G20\n') # inches
        for c in contours:
            f.write('(new contour)\n')
            for i in range(len(c)):
                x, y = c[i][0]
                speed = 10000
                f.write("G1 X{} Y{} F{}\n".format(x,y, speed))


if __name__ == "__main__":
    image = "../ext/nicolas_silhouette.png"

    im = cv2.imread(image)
    imgray = cv2.cvtColor(im, cv2.COLOR_BGR2GRAY)
    ret, thresh = cv2.threshold(imgray, 127, 255, 0)
    contours, hierarchy = cv2.findContours(thresh, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)
    cont = cv2.drawContours(im, contours, -1, (0,255,0), 3)
    contour_to_gcode(contours, image)


    cv2.imshow('Edges in the image', cont) #displaying the image
    cv2.waitKey(0)
