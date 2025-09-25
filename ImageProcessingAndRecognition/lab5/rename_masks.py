import os

from tqdm import tqdm

data_dir = 'data/masks'

for filename in tqdm(os.listdir(data_dir)):
    if '_segmentation' in filename:
        new_filename = filename.replace('_segmentation', '')
        os.rename(os.path.join(data_dir, filename), os.path.join(data_dir, new_filename))
