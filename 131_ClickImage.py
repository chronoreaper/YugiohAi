#Source
#https://medium.com/@martin.lees/image-recognition-for-automation-with-python-711ac617b4e5
import cv2
import numpy as np
import pyautogui
import random
import time
import platform
import subprocess
import sys

is_retina = False
noFoundLoop=0

if platform.system() == "Darwin":
    is_retina = subprocess.call("system_profiler SPDisplaysDataType | grep 'retina'", shell=True)

def imagesearch(image, precision=0.8):
    im = pyautogui.screenshot()
    if is_retina:
        im.thumbnail((round(im.size[0] * 0.5), round(im.size[1] * 0.5)))
    # im.save('testarea.png') useful for debugging purposes, this will save the captured region as "testarea.png"
    img_rgb = np.array(im)
    img_gray = cv2.cvtColor(img_rgb, cv2.COLOR_BGR2GRAY)
    template = cv2.imread(image, 0)
    template.shape[::-1]

    res = cv2.matchTemplate(img_gray, template, cv2.TM_CCOEFF_NORMED)
    min_val, max_val, min_loc, max_loc = cv2.minMaxLoc(res)
    if max_val < precision:
        return [-1, -1]
    return max_loc


pos = imagesearch(sys.argv[1])
pos = imagesearch(sys.argv[1])
im = cv2.imread(sys.argv[1])
#height width channel
h, w, c = im.shape

if pos[0] != -1:
    #print("\nposition : ", pos[0], pos[1])
    pyautogui.moveTo(pos[0]+w/2, pos[1]+h/2)
    pyautogui.click(button="left")
else:
    print("image not found") 