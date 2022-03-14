#!/usr/bin/env python3
import logging
import os
import subprocess
import sys
import gphoto2 as gp
import datetime

logging.basicConfig(
        format='%(levelname)s: %(name)s: %(message)s', level=logging.WARNING)
callback_obj = gp.check_result(gp.use_python_logging())

def get_camera():
    camera = gp.Camera()
    camera.init()
    return camera

def capture_image_from_dslr():
    camera = get_camera()
    capture_image(camera)
    camera.exit()
    return 0

def capture_image(camera):
    print('Capturing image')
    file_path = camera.capture(gp.GP_CAPTURE_IMAGE)
    print('Camera file path: {0}/{1}'.format(file_path.folder, file_path.name))
    file_name, extension = file_path.name.split(".")
    now = datetime.datetime.now()
    date_time = now.strftime("%Y%m%d-%H%M%S")
    target = os.path.join('../ext/', "foamo_" + date_time + "." + extension)

    camera_file = camera.file_get(
        file_path.folder, file_path.name, gp.GP_FILE_TYPE_NORMAL)
    camera_file.save(target)
    return 0

if __name__ == "__main__":
    sys.exit(capture_image_from_dslr())
