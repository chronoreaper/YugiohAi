import datetime
import glob
import math
import os
import shutil
import sqlite3
import string
import subprocess
import sys
import time
import random
import typing
import csv
import numpy as np
import pickle
import itertools
from sklearn.model_selection import train_test_split
from sklearn.metrics import accuracy_score

import torch
import numpy as np
from torch.utils.data import Dataset, DataLoader
import torch.nn as nn
import torch.nn.functional as F
from torch.utils.data.dataloader import default_collate

from sys import platform
from pathlib import Path

dtype = torch.float
device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')

class Data(Dataset):
  def __init__(self, X_train, y_train):
    # need to convert float64 to float32 else 
    # will get the following error
    # RuntimeError: expected scalar type Double but found Float
    self.X = torch.from_numpy(X_train.astype(np.float32))
    # need to convert float64 to Long else 
    # will get the following error
    # RuntimeError: expected scalar type Long but found Float
    self.y = torch.from_numpy(y_train).type(torch.LongTensor)
    self.len = self.X.shape[0]
  
  def __getitem__(self, index):
    return self.X[index], self.y[index]
  
  def __len__(self):
    return self.len

import torch.nn as nn

class Network(nn.Module):
  def __init__(self, input_dim, output_dim):
    super(Network, self).__init__()
    hidden_layers = (input_dim + output_dim) * 2

    self.layer1 = nn.Linear(input_dim, hidden_layers)
    self.layer2 = nn.Linear(hidden_layers, hidden_layers)
    self.output = nn.Linear(hidden_layers, output_dim)


    self.single = nn.Linear(input_dim, output_dim)
    self.dropout1 = nn.Dropout(0.9)
    self.dropout2 = nn.Dropout(0.2)
    self.act1 = nn.Tanh()
    self.act2 = nn.ReLU()
    self.act0 = nn.Sigmoid()

  def forward(self, x):
    #x = self.single(x)
    x = self.layer1(x)
    #x = self.dropout1(x)
    #x = self.act1(x)

    x = self.layer2(x)
    x = self.act2(x)

    x = self.output(x)
    x = self.act0(x)
    return x

# x_train = [[1,1],[1,0],[0,1],[0,0]]
# y_train = [[1,1],[1,0],[-1,0],[-1,-1]]

# x_train = [[1,1],  [1,0],  [1,0],  [0,1],  [0,0]]
# y_train = [[-1,-1,1],[1,-1,-1],[-1,1,-1],[-1,1,-1],[-1,0,-1]]

# x_train = [[1,1],    [1,1],    [1,0],    [1,0],  [0,1],  [0,1],     [0,0]]
# y_train = [[-1,-1,1],[-1,0,-1],[1,-1,0],[-1,1,0],[-1,1,-1],[-1,0,-1],[-1,0,-1]]


x_train = [[1,1,1], [1,1,0], [0,1,1], [1,0,1], [1,0,0], [0,1,0], [0,0,1], [0,0,0]]
y_train = [[1,1,1], [1,1,0], [-1,0,0], [1,-1,1], [1,-1,-1], [-1,1,-1], [-1,-1,0], [-1,-1,-1]]

traindata = Data(np.array(x_train), np.array(y_train))
batch_size = min(40, len(y_train))#len(y_train)
trainloader = DataLoader(traindata, batch_size=batch_size, shuffle=True, collate_fn=lambda x: tuple(x_.to(device) for x_ in default_collate(x)))
clf = Network(3, 3)
#clf = Network(input_length + output_length, 1, bias)
clf.to(device)


criterion = nn.BCEWithLogitsLoss().cuda()
#criterion = nn.BCEWithLogitsLoss().cuda()
#criterion = nn.CrossEntropyLoss().cuda()
optimizer = torch.optim.Adam(clf.parameters(), lr=0.01)

epochs = 100
for epoch in range(epochs):
  y_true = []
  y_pred = []
  running_loss = 0.0
  for i, data in enumerate(trainloader):
    inputs, labels = data
    inputs, labels = inputs.to(device), labels.to(device)
    
    clf.train()
    # forward propagation
    outputs = clf(inputs)
    #outputs = torch.sigmoid(outputs)
    #outputs = torch.softmax(outputs, 1)

    # _,indexes = np.where(labels.cpu() != -1)
    # indexes = torch.from_numpy(indexes).to(device)
    # outputs2 = outputs.index_select(1,indexes)
    # labels2 = labels.index_select(1,indexes)
    mask = (labels.cpu() != -1).to(device)
    outputs2 = outputs.masked_select(mask)
    labels2 = labels.masked_select(mask)

    loss = criterion(outputs2, labels2.float())
    #loss = criterion(outputs, torch.sigmoid(labels.float()))
    #loss = criterion(outputs, labels.unsqueeze(1).float())

    # set optimizer to zero grad to remove previous epoch gradients
    optimizer.zero_grad()
    # backward propagation
    loss.backward()
    # optimize
    optimizer.step()

    running_loss += loss.item()

    #PREDICTIONS 
    clf.eval()
    with torch.no_grad():
      pred = np.round(outputs.cpu().detach().numpy())
      labels = np.round(labels.cpu().detach().numpy())         
      # y_pred = pred.tolist()
      # y_true = labels.tolist()
      y_pred.extend(pred.tolist())
      y_true.extend(labels.tolist())

  #if epoch % 100 == 99:
    # display statistics
  
  #print(f"[{epoch + 1}, {i + 1:5d}]Accuracy on training set is " + str(accuracy_score(y_true,np.array(y_pred))))
  #print(f'[{epoch + 1}, {i + 1:5d}] loss: {running_loss / (i + 1):.5f}')

clf.eval()
print([0,0,1])
print(clf(torch.from_numpy(np.array([0,0,1])).to(device).float()))
print([0,1,0])
print(clf(torch.from_numpy(np.array([0,1,0])).to(device).float()))
print([1,0,0])
print(clf(torch.from_numpy(np.array([1,0,0])).to(device).float()))
print([0,0,0])
print(clf(torch.from_numpy(np.array([0,0,0])).to(device).float()))