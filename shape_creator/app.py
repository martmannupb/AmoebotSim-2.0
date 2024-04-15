"""
SHAPE CREATOR UTILITY

HOW TO USE THIS:
================

Install Pygame (this was made with version 2.5.2):
https://www.pygame.org/wiki/GettingStarted

Double-click the script file or call "python3 app.py" in a terminal

You can exit the application by pressing ESCAPE or closing the window.
The window can be resized.

Pan the camera around by moving the mouse while holding the right mouse button.
Zoom in and out using the scroll wheel.

Press TAB to cycle through the modes. The available modes are
BASIC, SNOWFLAKE and STAR_CONVEX.

In BASIC mode, there is only a single shape which you can edit
by clicking adjacent elements. Hold CTRL and click to remove an element.

In SNOWFLAKE mode, there is one root snowflake to which you can add arms.
Select a snowflake by clicking its highlighted origin and add arms by left
clicking. A preview of the arm will be shown before you click.
Shift-click onto an empty node to add a new snowflake.
To add a child to an existing snowflake, select the parent snowflake,
then select the edge to which the child should be added, then select
the child snowflake's origin. A preview of the new shape will be shown.
While the preview is visible, press the Q and E keys to change the child's
relative rotation. Press RETURN to confirm the placement.
While a snowflake is selected, you can move it around using the arrow keys.
This does not work for the root snowflake.

In STAR_CONVEX mode, there is only a single shape. You can add elements
by clicking on any node, edge or face. The shape will be extended to contain
that element so that it remains star convex. Removing elements is not
supported. Press TAB thrice to start with a new shape.


Press the C key to copy a JSON description of your current shape to
your clipboard. The string will be as short as possible and it can be
entered directly into the simulator's shape input field.

Alternatively, press the S key to save the JSON description in a file
next to this script file. Place the file in the simulator's
Assets/Shapes/ folder and enter the file name into the shape input
field.

To print the nodes, edges and faces of the current shape to the
command line, press P.


This script is not designed to be as efficient or maintainable as
possible. It merely provides convenient shape creation functionality.
(The script was made with Pygame version 2.5.2 on a Windows 11 system.
Compatibility with other versions or operating systems was not tested.)
"""





import math
import os
import sys

import json

from collections import deque

import pygame
from pygame.locals import *
from pygame import gfxdraw


# Initialization
pygame.init()


# Constants
SQRT3 = math.sqrt(3.0)
H = SQRT3 / 2				# Vertical distance between rows

MOUSE_L = 1
MOUSE_M = 2
MOUSE_R = 3

DRAG_BUTTON = MOUSE_R
PLACE_BUTTON = MOUSE_L
KEY_ADD = K_LSHIFT
KEY_REMOVE = K_LCTRL
KEY_LEFT = K_q
KEY_RIGHT = K_e
KEY_CONFIRM = K_RETURN
KEY_QUIT = K_ESCAPE
KEY_MOVE_L = K_LEFT
KEY_MOVE_R = K_RIGHT
KEY_MOVE_U = K_UP
KEY_MOVE_D = K_DOWN
KEY_CLIPBOARD = K_c
KEY_SAVE = K_s
KEY_PRINT = K_p
MOVE_KEYS = [KEY_MOVE_L, KEY_MOVE_R, KEY_MOVE_U, KEY_MOVE_D]

DIR_VECS = [(1, 0), (0, 1), (-1, 1), (-1, 0), (0, -1), (1, -1)]

# Shape types (from simulator)
TRIANGLE = 0
PARALLELOGRAM = 1
TRAPEZOID = 2
PENTAGON = 3


_DIRNAME = os.path.dirname(os.path.realpath(__file__))
SAVE_FILE_BASE = os.path.join(_DIRNAME, "shape.json")
SAVE_FILE_SNOWFLAKE = os.path.join(_DIRNAME, "snowflake.json")
SAVE_FILE_STAR_CONVEX = os.path.join(_DIRNAME, "star_convex.json")


# Colors
WHITE = (255, 255, 255)
GRAY = (128, 128, 128)
RED = (255, 0, 0)
GREEN = (0, 255, 0)
BLUE = (0, 0, 255)

BG_COLOR = (200, 200, 200)

SEL_COLOR = (160, 160, 160)
SEL_COLORA = (160, 160, 160, 128)

COLOR_NODE = BLUE
COLOR_EDGE = (0, 90, 255)
COLOR_FACE = (0, 128, 255, 128)
COLOR_ROOT = (192, 32, 32)
COLOR_ROOT_SEL = (32, 192, 32)
COLOR_EDGE_SEL = (32, 192, 32)

COLOR_NODE_PREV = (64, 192, 64)
COLOR_EDGE_PREV = (64, 192, 128)
COLOR_FACE_PREV = (64, 255, 128, 128)


# Clock setup
FPS = 60


# Initial screen setup
SCREEN_WIDTH = 900
SCREEN_HEIGHT = 600


# Zoom
BASE_SCALE = 100	# How many pixels are one grid unit for zoom factor 1
ZOOM_MIN = 0.25		# Min zoom factor
ZOOM_MAX = 10		# Max zoom factor
ZOOM_FAC = 1.1		# Value to multiply or divide zoom factor with


# Helper classes to store data in objects instead of global variables

# Screen information
class Screen:
	def __init__(self):
		self.width = SCREEN_WIDTH
		self.height = SCREEN_HEIGHT
		# Offset for drawing relative to the center of the screen (floats)
		self.offset_x = SCREEN_WIDTH / 2
		self.offset_y = SCREEN_HEIGHT / 2
		self.displaysurf = pygame.display.set_mode((self.width, self.height), RESIZABLE)
		pygame.display.set_caption("Snowflake Creator")

	def change_size(self, width, height):
		self.width = width
		self.height = height
		self.offset_x = self.width / 2
		self.offset_y = self.height / 2
		self.displaysurf = pygame.display.set_mode((self.width, self.height), RESIZABLE)


# Camera with zoom
class Camera:
	def __init__(self):
		self.x = 0
		self.y = 0
		self.dragging = False
		self.last_mouse_x = 0
		self.last_mouse_y = 0
		self.scale = BASE_SCALE
		self.zoom = 1
		self.zoom_min = ZOOM_MIN
		self.zoom_max = ZOOM_MAX
		self.zoom_fac = ZOOM_FAC

	def update_zoom(self, change):
		# Update zoom factor
		if change > 0:
			self.zoom *= self.zoom_fac**change
			self.zoom = min(self.zoom, self.zoom_max)
		elif change < 0:
			self.zoom /= self.zoom_fac**(-change)
			self.zoom = max(self.zoom, self.zoom_min)



# Setup global objects
clock = pygame.time.Clock()
screen = Screen()
cam = Camera()

pygame.scrap.init()


# Helpers - Take pixel coordinates relative to screen center
def draw_line(start_x, start_y, end_x, end_y, width, color):
	pygame.draw.line(screen.displaysurf, color, (start_x + screen.offset_x, -start_y + screen.offset_y), (end_x + screen.offset_x, -end_y + screen.offset_y), int(width))


def draw_circle(pos_x, pos_y, radius, color, filled = True):
	x = int(pos_x + screen.offset_x)
	y = int(-pos_y + screen.offset_y)
	r = int(radius)
	pygame.gfxdraw.aacircle(screen.displaysurf, x, y, r, color)
	if filled:
		pygame.gfxdraw.filled_circle(screen.displaysurf, x, y, r, color)


def draw_polygon(points, color, width = 0):
	pygame.draw.polygon(screen.displaysurf, color, [(x + screen.offset_x, -y + screen.offset_y) for (x, y) in points], int(width))


def draw_tri(a, b, c, color):
	pygame.gfxdraw.filled_trigon(
		screen.displaysurf,
		int(a[0] + screen.offset_x),
		int(-a[1] + screen.offset_y),
		int(b[0] + screen.offset_x),
		int(-b[1] + screen.offset_y),
		int(c[0] + screen.offset_x),
		int(-c[1] + screen.offset_y),
		color
	)


def draw_background():
	scale = cam.scale * cam.zoom
	width = 5 * cam.zoom
	# Convert camera coordinates back into pixel coordinates
	cam_x = cam.x * cam.zoom
	cam_y = cam.y * cam.zoom

	# Horizontal lines
	n_lines = math.ceil(screen.height / scale / H) + 1
	# Lowest line index y is largest value y such that y * H <= (cam_y - screen.height / 2) / scale
	# y * H <= (cam_y - screen.height / 2) / scale
	# <=>
	# y <= (cam_y - screen.height / 2) / scale / H
	y = math.floor((cam_y - screen.height / 2) / scale / H)
	x1 = - screen.width / 2
	x2 = screen.width / 2
	for i in range(y, y + n_lines):
		line_y = i * H * scale - cam_y
		draw_line(x1, line_y, x2, line_y, width, BG_COLOR)

	# Diagonal lines
	# Line equation for lines going up: l_i(x) = SQRT3 * (x - i)
	# Left line index: Largest i for which l_i((cam_x - screen.width / 2) / scale) >= (cam_y + screen.height / 2) / scale
	# <=>
	# SQRT3 * ((cam_x - screen.width / 2) / scale - i) >= (cam_y + screen.height / 2) / scale
	# <=>
	# (cam_x - screen.width / 2) / scale - i >= (cam_y + screen.height / 2) / scale / SQRT3
	# <=>
	# i <= (cam_x - screen.width / 2) / scale - (cam_y + screen.height / 2) / scale / SQRT3
	# <=>
	# i <= (cam_x - screen.width / 2 - (cam_y + screen.height / 2) / SQRT3) / scale
	left = math.floor((cam_x - screen.width / 2 - (cam_y + screen.height / 2) / SQRT3) / scale)
	# Right line index: Smallest i for which l_i((cam_x + screen.width / 2) / scale) <= (cam_y - screen.height / 2) / scale
	# <=> ...
	# i >= (cam_x + screen.width / 2 - (cam_y - screen.height / 2) / SQRT3) / scale
	right = math.ceil((cam_x + screen.width / 2 - (cam_y - screen.height / 2) / SQRT3) / scale)
	x1 = (cam_x - screen.width / 2) / scale
	x2 = (cam_x + screen.width / 2) / scale
	x1_rel = -screen.width / 2
	x2_rel = screen.width / 2
	for i in range(left, right + 1):
		y1 = SQRT3 * (x1 - i)
		y2 = SQRT3 * (x2 - i)
		y1_rel = y1 * scale - cam_y
		y2_rel = y2 * scale - cam_y
		draw_line(x1_rel, y1_rel, x2_rel, y2_rel, width, BG_COLOR)

	# Symmetrical for lines going down
	# Equation: l_i(x) = -SQRT3 * (x - i)
	# Left line index: Largest i for which l_i((cam_x - screen.width / 2) / scale) <= (cam_y - screen.height / 2) / scale
	# <=>
	# -SQRT3 * ((cam_x - screen.width / 2) / scale - i) >= (cam_y - screen.height / 2) / scale
	# <=>
	# (cam_x - screen.width / 2) / scale - i <= (cam_y - screen.height / 2) / scale / (-SQRT3)
	# <=>
	# i >= (cam_x - screen.width / 2) / scale - (cam_y - screen.height / 2) / scale / (-SQRT3)
	# <=>
	# i >= (cam_x - screen.width / 2 - ((cam_y - screen.height / 2) / (-SQRT3))) / scale
	left = math.floor((cam_x - screen.width / 2 - ((cam_y - screen.height / 2) / (-SQRT3))) / scale)
	# Right line index: Smallest i for which l_i((cam_x + screen.width / 2) / scale) >= (cam_y + screen.height / 2) / scale
	# <=> ...
	# i <= (cam_x + screen.width / 2 - ((cam_y + screen.height / 2) / (-SQRT3))) / scale
	right = math.ceil((cam_x + screen.width / 2 - ((cam_y + screen.height / 2) / (-SQRT3))) / scale)
	for i in range(left, right + 1):
		y1 = -SQRT3 * (x1 - i)
		y2 = -SQRT3 * (x2 - i)
		y1_rel = y1 * scale - cam_y
		y2_rel = y2 * scale - cam_y
		draw_line(x1_rel, y1_rel, x2_rel, y2_rel, width, BG_COLOR)


# Grid to world coordinates
def gtw(x, y):
	return (x + y / 2, y * H)


# World to screen pixel coordinates
def wts(x, y):
	return (x * cam.scale - cam.x) * cam.zoom, (y * cam.scale - cam.y) * cam.zoom


# Rotates the given grid coordinates by rot 60 degree rotations counterclockwise
def rotate_grid_point(x, y, rot):
	vec_x = DIR_VECS[rot]
	vec_y = DIR_VECS[(rot + 1) % 6]
	x1 = vec_x[0] * x
	y1 = vec_x[1] * x
	x2 = vec_y[0] * y
	y2 = vec_y[1] * y
	return x1 + x2, y1 + y2


# Finds the grid/shape element that is currently highlighted by the cursor
# Returns a tuple (type, nodes) where type is 0 for a node, 1 for an edge and 2 for a face
# and nodes is either a single node or a list of 2 or 3 nodes, resp.
def find_highlighted_element(x, y):
	# There is probably a much easier way to do this...

	# Convert to world coordinates
	# First, pixel coordinates relative to center of screen
	x -= screen.offset_x
	y = screen.height - y - screen.offset_y
	# Divide away scaling (now world distance from center of screen)
	s = cam.scale * cam.zoom
	x /= s
	y /= s
	# Add camera offset (camera coordinates do not take scale into account, only zoom)
	xw = x + cam.x / cam.scale
	yw = y + cam.y / cam.scale

	# Find nearest grid cell using the same technique as in AmoebotSim 2.0
	# First convert to grid coordinates and add a z coordinate as negative sum of x and y
	yg = yw / H
	xg = xw - yg / 2
	zg = - xg - yg

	# Round each to nearest int
	(xi, yi, zi) = (round(n) for n in (xg, yg, zg))
	# Compute absolute differences
	dx = abs(xg - xi)
	dy = abs(yg - yi)
	dz = abs(zg - zi)
	# May have to change coordinates based on which difference is largest
	if dx > dy and dx > dz:
		xi = - yi - zi
	elif dy > dz:
		yi = -xi - zi

	x, y = xi, yi

	# DEBUG: Draw circle at nearest node
	# xx, yy = wts(*gtw(x, y))
	# draw_circle(xx, yy, 15, GRAY)

	# Check in which half-spaces of the 3 axes we are (use world coordinates for this, not grid)
	pwx, pwy = gtw(x, y)
	# Horizontal: Only have to compare y value
	above_horizontal = yw >= pwy
	# Lines going up
	# y = SQRT3 * (x - i)  <=>  y / SQRT3 = x - i  <=>  i = x - y / SQRT3
	i = pwx - pwy / SQRT3
	below_up_line = SQRT3 * (xw - i)  >= yw
	# Lines going down
	# y = -SQRT3 * (x - i)  <=>  y / (-SQRT3) = x - i  <=>  i = x + y / SQRT3
	i = pwx + pwy / SQRT3
	above_down_line = yw + SQRT3 * (xw - i) >= 0

	neighbors = [(x + 1, y), (x, y + 1), (x - 1, y + 1), (x - 1, y), (x, y - 1), (x + 1, y - 1)]
	if above_horizontal:
		if below_up_line:
			# First triangle
			nbr = 0
		elif above_down_line:
			# Second triangle
			nbr = 1
		else:
			# Third triangle
			nbr = 2
	else:
		if not below_up_line:
			# Fourth triangle
			nbr = 3
		elif not above_down_line:
			# Fifth triangle
			nbr = 4
		else:
			# Sixth triangle
			nbr = 5
	nbr1 = neighbors[nbr]
	nbr2 = neighbors[(nbr + 1) % 6]

	# DEBUG: Draw the two triangle neighbors
	# xx, yy = wts(*gtw(nbr1[0], nbr1[1]))
	# draw_circle(xx, yy, 12, GRAY)
	# xx, yy = wts(*gtw(nbr2[0], nbr2[1]))
	# draw_circle(xx, yy, 12, GRAY)

	# Compute the inbetween points
	n1 = gtw(nbr1[0], nbr1[1])
	n2 = gtw(nbr2[0], nbr2[1])
	e1 = ((pwx + n1[0]) / 2, (pwy + n1[1]) / 2)
	e2 = ((pwx + n2[0]) / 2, (pwy + n2[1]) / 2)
	e3 = ((n1[0] + n2[0]) / 2, (n1[1] + n2[1]) / 2)
	tri = ((pwx + n1[0] + n2[0]) / 3, (pwy + n1[1] + n2[1]) / 3)

	# DEBUG: Draw all extra points
	# draw_circle(*(wts(*e1)), 8, GRAY)
	# draw_circle(*(wts(*e2)), 8, GRAY)
	# draw_circle(*(wts(*e3)), 8, GRAY)
	# draw_circle(*(wts(*tri)), 10, GRAY)

	# Find out which point is closest
	points = [(pwx, pwy), n1, n2, e1, e2, e3, tri]
	dist = math.pow(pwx - xw, 2) + math.pow(pwy - yw, 2)
	i = 0
	for j in range(1, 7):
		px, py = points[j]
		d = math.pow(px - xw, 2) + math.pow(py - yw, 2)
		if d < dist:
			dist = d
			i = j

	# draw_circle(*wts(*points[i]), 10, GREEN)
	if i < 3:
		return (0, [(x, y), nbr1, nbr2][i])
	elif i == 3:
		return (1, [(x, y), nbr1])
	elif i == 4:
		return (1, [(x, y), nbr2])
	elif i == 5:
		return (1, [nbr1, nbr2])
	else:
		return (2, [(x, y), nbr1, nbr2])


def draw_highlighted_element(result):
	nodes = []
	edges = []
	faces = []
	if result[0] == 0:
		# Only one node
		p = result[1]
		nodes.append(wts(*gtw(*p)))
	elif result[0] == 1:
		# Two nodes with an edge
		p1 = result[1][0]
		p2 = result[1][1]
		nodes.append(wts(*gtw(*p1)))
		nodes.append(wts(*gtw(*p2)))
		edges.append((nodes[0], nodes[1]))
	else:
		# Whole triangle
		p1 = result[1][0]
		p2 = result[1][1]
		p3 = result[1][2]
		nodes.append(wts(*gtw(*p1)))
		nodes.append(wts(*gtw(*p2)))
		nodes.append(wts(*gtw(*p3)))
		edges.append((nodes[0], nodes[1]))
		edges.append((nodes[0], nodes[2]))
		edges.append((nodes[1], nodes[2]))
		faces.append((nodes[0], nodes[1], nodes[2]))
	for face in faces:
		draw_tri(*face, SEL_COLORA)
	for edge in edges:
		draw_line(*edge[0], *edge[1], 6 * cam.zoom, SEL_COLOR)
	for node in nodes:
		draw_circle(*node, 8 * cam.zoom, SEL_COLOR)


# Shape classes
class Shape:
	def __init__(self, pos, nodes, edges, faces, node_col, edge_col, face_col):
		self.pos = pos			# Grid position (x, y)
		self.nodes = nodes		# List of grid positions relative to (x, y)
		self.edges = edges		# List of index tuples referencing nodes
		self.faces = faces		# List of index triples referencing nodes
		self.node_col = node_col
		self.edge_col = edge_col
		self.face_col = face_col
		self.nodes_tf = []
		self.recalculate_points()


	def print_shape(self):
		print(self.nodes)
		print(self.edges)
		print(self.faces)


	# Applies the coordinate transformation to all points to save time
	def recalculate_points(self, scale = 1):
		self.nodes_tf = []
		for n in self.nodes:
			x, y = self.transform(n[0], n[1], scale)
			self.nodes_tf.append((x, y))


	def transform(self, x, y, scale = 1):
		x, y = x * scale + self.pos[0], y * scale + self.pos[1]
		x, y = gtw(x, y)
		return x, y

	# {"nodes":[{"x":0,"y":0},{"x":0,"y":1},{"x":1,"y":0}],"edges":[{"u":0,"v":1},{"u":0,"v":2},{"u":1,"v":2}],"faces":[{"u":0,"v":1,"w":2}],"traversal":[]}
	# {"nodes": [{"x": 0, "y": 0}, {"x": 0, "y": 1}, {"x": 1, "y": 0}, {"x": 1, "y": -1}, {"x": 2, "y": -1}, {"x": 1, "y": 1}, {"x": 2, "y": 0}], "edges": [{"u": 1, "v": 0}, {"u": 1, "v": 2}, {"u": 0, "v": 2}, {"u": 2, "v": 3}, {"u": 2, "v": 4}, {"u": 3, "v": 4}, {"u": 5, "v": 2}, {"u": 5, "v": 6}, {"u": 2, "v": 6}], "faces": [{"u": 1, "v": 0, "w": 2}, {"u": 2, "v": 3, "w": 4}, {"u": 5, "v": 2, "w": 6}]}

	def to_json(self, pretty=False):
		s = json.dumps(
		{
			"shape":
			{
				"nodes": [{"x": n[0], "y": n[1]} for n in self.nodes],
				"edges": [{"u": e[0], "v": e[1]} for e in self.edges],
				"faces": [{"u": f[0], "v": f[1], "w": f[2]} for f in self.faces]
			},
			"constituents": [],
			"dependencyTree": []
		},
		indent=4 if pretty else None)
		return s


	def draw(self):
		scale = cam.scale * cam.zoom
		# Convert camera coordinates back into pixel coordinates
		cam_x = cam.x * cam.zoom
		cam_y = cam.y * cam.zoom
		# First draw all faces
		for triple in self.faces:
			points = [self.nodes_tf[i] for i in triple]
			points = [(scale * x - cam_x, scale * y - cam_y) for (x, y) in points]
			draw_tri(*points, self.face_col)

		# Then draw the edges
		for edge in self.edges:
			points = [self.nodes_tf[i] for i in edge]
			points = [(scale * x - cam_x, scale * y - cam_y) for (x, y) in points]
			draw_line(*points[0], *points[1], 7 * cam.zoom, self.edge_col)

		# Finally draw the nodes
		for node in self.nodes_tf:
			x, y = scale * node[0] - cam_x, scale * node[1] - cam_y
			draw_circle(x, y, 10 * cam.zoom, self.node_col)


	def move_pos(self, x, y):
		self.pos = (self.pos[0] + x, self.pos[1] + y)
		self.recalculate_points()


	def contains_edge(self, edge):
		for e in self.edges:
			if e[0] == edge[0] and e[1] == edge[1] or e[0] == edge[1] and e[1] == edge[0]:
				return True
		return False


	def contains_face(self, face):
		for f in self.faces:
			if set(f) == set(face):
				return True
		return False


	# Adds the given edge (pair of global coordinates) if it is not in the shape yet
	def add_edge(self, edge):
		# Get the two nodes and convert them to local coordinates
		p1, p2 = edge[0], edge[1]
		p1 = (p1[0] - self.pos[0], p1[1] - self.pos[1])
		p2 = (p2[0] - self.pos[0], p2[1] - self.pos[1])
		# Check if we already contain one or both of the nodes
		i = 0
		j1 = -1
		j2 = -1
		for node in self.nodes:
			if node == p1:
				j1 = i
			if node == p2:
				j2 = i
			i += 1
		if j1 == -1 and j2 == -1:
			# We contain none of the two nodes, abort
			return
		elif j1 != -1 and j2 != -1:
			# We contain both of the nodes, only add edge if we don't have it yet
			if not self.contains_edge((j1, j2)):
				self.edges.append((j1, j2))
		else:
			# Add the missing node and edge
			new_idx = len(self.nodes)
			p = p1 if j1 == -1 else p2
			self.nodes.append(p)
			self.nodes_tf.append(self.transform(*p))
			self.edges.append((j1 if j1 != -1 else j2, new_idx))


	# Adds the given face (triple of global coordinates) if it is not in the shape yet
	def add_face(self, face):
		# Get the three nodes and convert them to local coordinates
		p1, p2, p3 = face[0], face[1], face[2]
		p1 = (p1[0] - self.pos[0], p1[1] - self.pos[1])
		p2 = (p2[0] - self.pos[0], p2[1] - self.pos[1])
		p3 = (p3[0] - self.pos[0], p3[1] - self.pos[1])
		points = [p1, p2, p3]
		# Check which of these points we already contain
		i = 0
		j1 = -1
		j2 = -1
		j3 = -1
		for node in self.nodes:
			if node == p1:
				j1 = i
			if node == p2:
				j2 = i
			if node == p3:
				j3 = i
			i += 1
		n_cont = len([j for j in (j1, j2, j3) if j != -1])
		if n_cont == 0:
			# We contain none of the nodes, abort
			return
		else:
			# Add all required nodes, edges and faces
			js = [j1, j2, j3]
			if n_cont < 3:
				for i in range(3):
					if js[i] == -1:
						# Have to add this node
						new_idx = len(self.nodes)
						self.nodes.append(points[i])
						self.nodes_tf.append(self.transform(*points[i]))
						js[i] = new_idx
			j1, j2, j3 = js
			# Now we have all nodes, check for edges
			for edge in [(j1, j2), (j1, j3), (j2, j3)]:
				if not self.contains_edge(edge):
					self.edges.append(edge)
			# Finally, add the face if we do not have it yet
			if not self.contains_face((j1, j2, j3)):
				self.faces.append((j1, j2, j3))


	# Removes the given node (global coordinates) and all incident edges and faces
	def remove_node(self, node):
		# Convert to local coordinates and check if we even have this node
		node = (node[0] - self.pos[0], node[1] - self.pos[1])
		try:
			i = self.nodes.index(node)
		except ValueError:
			return
		
		# Do not allow removing the origin
		if node == (0, 0):
			return

		# Collect all edges and faces that would have to be removed, including the neighbor nodes
		nbrs = []
		edges = []
		faces = []
		for j in range(len(self.edges)):
			edge = self.edges[j]
			if i in edge:
				edges.append(j)
				if i == edge[0]:
					nbrs.append(edge[1])
				else:
					nbrs.append(edge[0])
		for j in range(len(self.faces)):
			if i in self.faces[j]:
				faces.append(j)

		# Check whether the neighbors would still be connected if we removed the node and all edges
		# Suffices to check whether all neighbors are reachable from one of them
		if len(nbrs) > 1 and not self._nodes_connected(nbrs[0], nbrs[1:], [i], edges):
			return

		# Remove the node, all edges and all faces
		edges = [self.edges[j] for j in edges]
		faces = [self.faces[j] for j in faces]
		for edge in edges:
			self.edges.remove(edge)
		for face in faces:
			self.faces.remove(face)
		# We have to adjust all edge and face indices if we remove the node
		self._remove_node_direct(i)


	# Removes the given edge (tuple of global coordinates), and all incident faces
	def remove_edge(self, edge):
		# Get the two nodes and convert them to local coordinates
		p1, p2 = edge[0], edge[1]
		p1 = (p1[0] - self.pos[0], p1[1] - self.pos[1])
		p2 = (p2[0] - self.pos[0], p2[1] - self.pos[1])
		# Check if we already contain one or both of the nodes
		i = 0
		j1 = -1
		j2 = -1
		for node in self.nodes:
			if node == p1:
				j1 = i
			if node == p2:
				j2 = i
			i += 1
		if j1 == -1 or j2 == -1:
			# We do not have these nodes, return
			return
		# We contain both nodes: Check if we have the edge and whether the two
		# connected nodes would still be connected without this edge
		# First, get the edge index
		i = 0
		edge_idx = -1
		for e in self.edges:
			if e == (j1, j2) or e == (j2, j1):
				edge_idx = i
				break
			i += 1
		if edge_idx == -1:
			# We do not have the edge
			return
		# Try to remove the edge
		if self._nodes_connected(j1, [j2], [], [edge_idx]):
			# If it works: Also remove the two incident faces, if they exist
			incident_faces = []
			for face in self.faces:
				if j1 in face and j2 in face:
					incident_faces.append(face)
			for f in incident_faces:
				self.faces.remove(f)
			del self.edges[edge_idx]


	# Removes the given face (triple of global coordinates)
	def remove_face(self, face):
		# Get the three nodes and convert them to local coordinates
		p1, p2, p3 = face[0], face[1], face[2]
		p1 = (p1[0] - self.pos[0], p1[1] - self.pos[1])
		p2 = (p2[0] - self.pos[0], p2[1] - self.pos[1])
		p3 = (p3[0] - self.pos[0], p3[1] - self.pos[1])
		points = [p1, p2, p3]
		# Check which of these points we already contain
		i = 0
		j1 = -1
		j2 = -1
		j3 = -1
		for node in self.nodes:
			if node == p1:
				j1 = i
			if node == p2:
				j2 = i
			if node == p3:
				j3 = i
			i += 1
		n_cont = len([j for j in (j1, j2, j3) if j != -1])
		if n_cont < 3 or not self.contains_face((j1, j2, j3)):
			# We do not have this face, return
			return
		# Remove the face
		face = set((j1, j2, j3))
		for f in self.faces:
			if set(f) == face:
				self.faces.remove(f)
				return


	# Checks whether the shape is still connected after removing the given nodes and edges
	# n1: The start point of the search
	# target_nodes: The nodes that have to be reachable from n1
	# excluded_nodes: The nodes that should be removed
	# excluded_edges: The edges that should be removed
	def _nodes_connected(self, n1, target_nodes, excluded_nodes, excluded_edges):
		if n1 in target_nodes:
			target_nodes.remove(n1)
		if len(target_nodes) == 0:
			return
		# Perform a BFS starting at n1, trying to find all target nodes
		queue = deque([n1])
		visited = set()
		visited.add(n1)
		while len(target_nodes) > 0 and len(queue) > 0:
			n = queue.popleft()
			# Collect all unvisited neighbors reachable via edges
			for i in range(len(self.edges)):
				if i in excluded_edges:
					continue
				edge = self.edges[i]
				nbr = -1
				if edge[0] == n:
					nbr = edge[1]
				elif edge[1] == n:
					nbr = edge[0]
				if nbr not in excluded_nodes and nbr not in visited:
					if nbr in target_nodes:
						target_nodes.remove(nbr)
					queue.append(nbr)
					visited.add(nbr)
		return len(target_nodes) == 0


	# Deletes the node with index idx, updates all edge and face tuples
	def _remove_node_direct(self, idx):
		for j in range(len(self.edges)):
			(a, b) = self.edges[j]
			if a >= idx:
				a -= 1
			if b >= idx:
				b -= 1
			self.edges[j] = (a, b)
		for j in range(len(self.faces)):
			(a, b, c) = self.faces[j]
			if a >= idx:
				a -= 1
			if b >= idx:
				b -= 1
			if c >= idx:
				c -= 1
			self.faces[j] = (a, b, c)
		del self.nodes[idx]
		del self.nodes_tf[idx]


	# Merges all nodes, edges and faces of the given shape into this shape
	def merge_with(self, other):
		nodes = other.nodes
		edges = other.edges
		faces = other.faces
		# First of all, transform nodes to our origin
		pos = (other.pos[0] - self.pos[0], other.pos[1] - self.pos[1])
		nodes = [(n[0] + pos[0], n[1] + pos[1]) for n in nodes]

		# Then merge the nodes:
		# Collect the shared nodes and build a dictionary pointing to our node indices
		# Collect the new nodes and build a dictionary pointing to the new indices
		shared_dict = dict()
		new_dict = dict()
		new_nodes = []
		n = len(self.nodes)
		for i in range(len(nodes)):
			# Find the index of other's node i in our nodes
			j = -1
			node = nodes[i]
			for k in range(len(self.nodes)):
				if node == self.nodes[k]:
					j = k
					break
			if j != -1:
				# Already have this node
				shared_dict[i] = j
			else:
				# New node
				new_dict[i] = n
				new_nodes.append(node)
				n += 1

		# Finally, collect all edges and faces that we do not already have
		new_edges = []
		new_faces = []
		for edge in edges:
			a, b = edge
			a_shared = a in shared_dict
			b_shared = b in shared_dict
			an = shared_dict[a] if a_shared else new_dict[a]
			bn = shared_dict[b] if b_shared else new_dict[b]
			have_edge = False
			if a_shared and b_shared:
				# We have to check if we already have the edge
				if self.contains_edge((an, bn)):
					have_edge = True
			if not have_edge:
				new_edges.append((an, bn))
		for face in faces:
			shared = [x in shared_dict for x in face]
			fn = tuple(shared_dict[face[i]] if shared[i] else new_dict[face[i]] for i in range(3))
			have_face = False
			if shared[0] and shared[1] and shared[2]:
				# We have to check if we already have this face
				if self.contains_face(fn):
					have_face = True
			if not have_face:
				new_faces.append(fn)

		# Lastly, add all new nodes, edges and faces
		self.nodes.extend(new_nodes)
		for node in new_nodes:
			self.nodes_tf.append(self.transform(*node))
		self.edges.extend(new_edges)
		self.faces.extend(new_faces)


class Snowflake(Shape):
	def __init__(self, pos):
		super().__init__(pos, [(0, 0)], [], [], COLOR_NODE, COLOR_EDGE, COLOR_FACE)
		self.selected = False
		# TODO: The selected edge could also be represented by direction + distance...
		self.selected_edge = None
		self.selected_edge_start = 0	# The edge index of the inner of the two nodes
		self.selected_edge_dir = 0
		self.child_candidate = None		# The snowflake that we are currently previewing as a child of the selected edge
		self.child_rot = 0				# The rotation of the currently selected child candidate
		self.preview_shape = None		# The shape to be drawn as a preview for the current child candidate
		self.arms = [0, 0, 0, 0, 0, 0]	# Arm lengths in all 6 directions (after adding children)
		self.children = [[], [], [], [], [], []]	# One list of children for each direction
													# Each element is a triple (edge_idx (distance), rotation, child_sf)
		self.parents = set()			# Set of snowflakes that depend on us


	def to_json(self, pretty=False):
		dtree = self.gen_dependency_tree()
		s = json.dumps(
		{
			"shape":
			{
				"nodes": [{"x": n[0], "y": n[1]} for n in self.nodes],
				"edges": [{"u": e[0], "v": e[1]} for e in self.edges],
				"faces": [{"u": f[0], "v": f[1], "w": f[2]} for f in self.faces]
			},
			"constituents": [],
			"dependencyTree": dtree
		},
		indent=4 if pretty else None)
		return s


	def gen_dependency_tree(self):
		dt = []

		all_children = self.get_children_recursive()
		all_children.add(self)
		# Generate a topological ordering
		top_order = []
		while len(all_children) > 0:
			found_child = False
			for child in all_children:
				has_unfinished_child = False
				for c in child.get_children_recursive():
					if c not in top_order:
						has_unfinished_child = True
						break
				if not has_unfinished_child:
					# All children are part of the topological ordering, can now process this node
					found_child = True
					top_order.append(child)
					all_children.remove(child)
					break

			if not found_child:
				print("Error: Could not find topological ordering!")
				return None

		for child in top_order:
			d = {"arms": child.arms}
			children = []
			for i in range(6):
				for c in child.children[i]:
					children.append({
						"childIdx": top_order.index(c[2]),
						"direction": i,
						"distance": c[0],
						"rotation": c[1]
					})
			d["children"] = children
			dt.append(d)

		return dt


	def draw(self):
		# Selected edge highlight
		if self.selected_edge != None:
			edge = self.edges[self.selected_edge]
			points = self.nodes_tf[edge[0]], self.nodes_tf[edge[1]]
			scale = cam.scale * cam.zoom
			cam_x = cam.x * cam.zoom
			cam_y = cam.y * cam.zoom
			points = [(scale * x - cam_x, scale * y - cam_y) for (x, y) in points]
			draw_line(*points[0], *points[1], 20 * cam.zoom, COLOR_EDGE_SEL)

		# Shape
		super().draw()

		# Preview child shape
		if self.child_candidate != None:
			self.preview_shape.draw()

		# Root
		pos = wts(*gtw(*self.pos))
		size = (15 if self.selected else 12) * cam.zoom
		color = COLOR_ROOT_SEL if self.selected else COLOR_ROOT
		draw_circle(*pos, size, color)


	def draw_highlight(self, hle):
		if hle[0] == 0:
			# Hovering over node
			# Check if this node extends one of our arms
			direction, dist = self._test_node_arm(hle[1])
			if direction != -1 and dist > self.arms[direction]:
				# This node extends one of our arms: Draw highlight
				start = DIR_VECS[direction]
				d = self.arms[direction]
				start = (start[0] * d + self.pos[0], start[1] * d + self.pos[1])
				end = hle[1]
				start = wts(*gtw(*start))
				end = wts(*gtw(*end))
				draw_line(*start, *end, 5 * cam.zoom, SEL_COLOR)
				draw_circle(*end, 8 * cam.zoom, SEL_COLOR)


	def select(self):
		self.selected = True


	def deselect(self):
		self.selected = False
		self.deselect_edge()


	def deselect_edge(self):
		self.selected_edge = None
		self.selected_edge_start = 0
		self.selected_edge_dir = 0
		self.deselect_child()


	def deselect_child(self):
		self.child_candidate = None
		self.child_rot = 0
		self.preview_shape = None


	def add_parent(self, parent):
		self.parents.add(parent)


	def remove_parent(self, parent):
		if parent in self.parents:
			self.parents.remove(parent)


	# Does not recalculate the shape!
	def remove_child(self, child):
		# Remove all instances of that child
		for direction in range(6):
			children = []
			for c in self.children[direction]:
				if c[2] != child:
					children.append(c)
			self.children[direction] = children


	def get_children(self):
		children = set()
		for direction in range(6):
			for _, _, c in self.children[direction]:
				children.add(c)
		return children


	def get_parents(self):
		return self.parents


	def get_children_recursive(self):
		children = set(self.get_children())
		for c in self.get_children():
			children = children.union(c.get_children_recursive())
		return children


	def get_parents_recursive(self):
		parents = set(self.get_parents())
		for p in self.get_parents():
			parents = parents.union(p.get_parents_recursive())
		return parents


	def remove(self):
		# Tell all children that we are not a parent anymore
		for child in self.get_children():
			child.remove_parent(self)
		# Tell all parents to remove us
		for parent in self.get_parents():
			parent.remove_child(self)

		# Recalculate all parent shapes
		self.recalculate_parent_shapes()


	def gen_shifted_shape(self, pos, rot, shift_dir):
		# Start with our own points
		nodes = self.nodes.copy()
		edges = self.edges.copy()
		faces = self.faces.copy()

		# Rotate all nodes
		if rot > 0:
			for i in range(len(nodes)):
				nodes[i] = rotate_grid_point(*nodes[i], rot)

		vec = DIR_VECS[shift_dir]

		# Find all nodes that do not have a neighbor in the shift direction OR are not connected to it
		# Shift them and add the created edges parallel to the shift direction
		shift_nodes = []
		shifted_nodes = []
		shifted_edges = []
		n = len(nodes)
		for i in range(len(nodes)):
			node = nodes[i]
			shifted = (node[0] + vec[0], node[1] + vec[1])
			# Find the shifted node
			j = -1
			for k in range(len(nodes)):
				if nodes[k] == shifted:
					j = k
					break
			if j == -1:
				# Node does not exist, add node and edge
				shift_nodes.append(i)
				shifted_nodes.append(shifted)
				shifted_edges.append((i, n))
				n += 1
			else:
				# Node exists, check if the edge exists
				if not self.contains_edge((i, j)):
					# Do not have this edge, add it
					shifted_edges.append((i, j))

		all_nodes = nodes + shifted_nodes

		# Now find edges that are not parallel to the shift direction and add edges and faces
		add_edges = []
		add_faces = []
		for edge in edges:
			n1, n2 = nodes[edge[0]], nodes[edge[1]]
			v = (n2[0] - n1[0], n2[1] - n1[1])
			direction = DIR_VECS.index(v)
			if direction == shift_dir or (direction + 3) % 6 == shift_dir:
				continue

			# This edge is not parallel to the shift direction, so it will span a parallelogram
			# The nodes spanning it already exist (some may have been added by shifting)
			# We have to check two additional edges and two faces (and may have to add them)
			# The first edge results from simply shifting the current edge
			n1s = (n1[0] + vec[0], n1[1] + vec[1])
			n2s = (n2[0] + vec[0], n2[1] + vec[1])
			# Find the two node indices (in the original and the new nodes)
			i = -1
			j = -1
			for k in range(len(all_nodes)):
				node = all_nodes[k]
				if node == n1s:
					i = k
				if node == n2s:
					j = k

			# Check if the edge already exists
			edge_exists = False
			if i < len(all_nodes) and j < len(all_nodes):
				for e in edges + shifted_edges + add_edges:
					if e == (i, j) or e == (j, i):
						edge_exists = True
						break

			if not edge_exists:
				# Edge does not exist yet, have to add it
				add_edges.append((i, j))

			# Now find the other edge and the two faces
			# The edge either connects n1 to n2s or n2 to n1s
			e1 = (n2s[0] - n1[0], n2s[1] - n1[1])
			e2 = (n2[0] - n1s[0], n2[1] - n1s[1])
			if e1 in DIR_VECS:
				# n1 and n2s
				diag = (edge[0], j)
				f1 = (edge[0], j, edge[1])
				f2 = (edge[0], j, i)
			else:
				# n2 and n1s
				diag = (edge[1], i)
				f1 = (edge[1], i, edge[0])
				f2 = (edge[1], i, j)
			# Check if the edge and the two faces exist, otherwise add them
			have_edge = False
			for e in edges + add_edges:
				if e == diag or (e[1], e[0]) == diag:
					have_edge = True
					break
			if not have_edge:
				add_edges.append(diag)
			have_f1 = False
			have_f2 = False
			set_f1 = set(f1)
			set_f2 = set(f2)
			if have_edge:		# Only have to search for the faces if we already had the edge
				for f in faces + add_faces:
					set_f = set(f)
					if set_f == set_f1:
						have_f1 = True
					if set_f == set_f2:
						have_f2 = True
					if have_f1 and have_f2:
						break
			if not have_f1:
				add_faces.append(f1)
			if not have_f2:
				add_faces.append(f2)

		shape = Shape(pos, all_nodes, edges + shifted_edges + add_edges, faces + add_faces, COLOR_NODE_PREV, COLOR_EDGE_PREV, COLOR_FACE_PREV)
		return shape


	# Returns True if this click did something
	def handle_click(self, hle, snowflakes):
		if hle[0] == 0:
			# Node clicked
			return self._node_click(hle[1], snowflakes)
		elif hle[0] == 1:
			# Edge clicked
			return self._edge_click(hle[1])
		return False


	def _node_click(self, node, snowflakes):
		# CASE 1: Clicked on an arm extension node
		# Check if this node extends one of our arms
		direction, dist = self._test_node_arm(node)
		if direction != -1 and dist > self.arms[direction]:
			# This node extends one of our arms!
			d = self.arms[direction]
			vec = DIR_VECS[direction]
			start = (vec[0] * d, vec[1] * d)
			prev_idx = self.nodes.index(start)

			# Add the corresponding nodes and edges
			for i in range(d + 1, dist + 1):
				end = (vec[0] * i, vec[1] * i)
				new_idx = len(self.nodes)
				self.nodes.append(end)
				self.nodes_tf.append(self.transform(*end))
				self.edges.append((prev_idx, new_idx))
				prev_idx = new_idx
				start = end
			self.arms[direction] = dist
			# Update the parent shapes
			self.recalculate_parent_shapes()
			return True

		# CASE 2: Did not click on arm extension
		# Check if we have a selected node and clicked another snowflake
		if self.selected_edge != None:
			for sf in snowflakes:
				if sf == self:
					continue
				root = sf.pos
				if node == root:
					# We found the snowflake that was clicked
					# Make sure this does not induce a cyclic dependency
					children = sf.get_children_recursive()
					if self in children:
						return False
					# No cyclic dependency, add child candidate
					self.child_candidate = sf
					self.child_rot = 0
					# Generate the preview shape
					self._gen_child_preview()
					return True
		return False


	def _edge_click(self, edge):
		# Check if the edge belongs to one of our arms
		n1, n2 = edge
		dir1, dist1 = self._test_node_arm(n1)
		dir2, dist2 = self._test_node_arm(n2)
		if dir1 == -1 or dir2 == -1 or (dir1 != dir2 and dist1 != 0 and dist2 != 0) or abs(dist1 - dist2) != 1 or dist1 > self.arms[dir1] or dist2 > self.arms[dir2]:
			# One of the nodes does not belong to the same arm
			# or they have the wrong distance
			return False
		# Both nodes belong to the same arm
		direction = max(dir1, dir2)		# Need max for case that one is the root node (has dir 0)
		# Mark this edge as selected
		n1 = (n1[0] - self.pos[0], n1[1] - self.pos[1])
		n2 = (n2[0] - self.pos[0], n2[1] - self.pos[1])
		# Make sure n1 is the inner node
		if abs(n1[0]) > abs(n2[0]) or abs(n1[1]) > abs(n2[1]):
			n1, n2 = n2, n1
		i = -1
		j = -1
		for k in range(len(self.nodes)):
			node = self.nodes[k]
			if node == n1:
				i = k
			if node == n2:
				j = k
		idx = -1
		for k in range(len(self.edges)):
			edge = self.edges[k]
			if i in edge and j in edge:
				idx = k
				# Remember which of the two nodes is the inner one
				if i == edge[0]:
					self.selected_edge_start = 0
				else:
					self.selected_edge_start = 1
				break
		self.selected_edge = idx
		self.selected_edge_dir = direction
		self.child_candidate = None
		self.child_rot = 0
		self.preview_shape = None
		return True


	# Returns True if this click did something
	def handle_remove_click(self, hle):
		if hle[0] == 0:
			# Node clicked
			return self._node_remove_click(hle[1])
		elif hle[0] == 1:
			# Edge clicked
			return self._edge_remove_click(hle[1])
		return False


	def _node_remove_click(self, node):
		# Check if this node is on one of our arms
		direction, dist = self._test_node_arm(node)
		if direction != -1 and dist > 0 and dist == self.arms[direction]:
			# This node is at the end of one of our arms!
			# Remove the node and the edge connecting it to its predecessor
			# Convert to local coordinates first
			node = (node[0] - self.pos[0], node[1] - self.pos[1])
			# Find the predecessor node
			dir_vec = DIR_VECS[direction]
			pred = (node[0] - dir_vec[0], node[1] - dir_vec[1])
			# Find their indices
			i, j = -1, -1
			for k in range(len(self.nodes)):
				n = self.nodes[k]
				if n == node:
					i = k
				if n == pred:
					j = k

			# Remove the edge
			for k in range(len(self.edges)):
				edge = self.edges[k]
				if i in edge and j in edge:
					break
			del self.edges[k]
			# Remove the node
			self._remove_node_direct(i)
			# Update direction
			self.arms[direction] -= 1
			# Remove edge selection
			self.deselect_edge()

			# Remove the edge's children
			self._remove_children_from_edge(dist - 1, direction)

			# Have to recalculate the shape
			self.recalculate_shape()

			# Then recalculate the parent shapes
			self.recalculate_parent_shapes()

			return True
		return False


	def _edge_remove_click(self, edge):
		# Check if the edge is on one of our arms
		dir1, dist1 = self._test_node_arm(edge[0])
		dir2, dist2 = self._test_node_arm(edge[1])
		if dir1 == -1 or dir2 == -1 or (dir1 != dir2 and dist1 != 0 and dist2 != 0) or abs(dist1 - dist2) != 1 or dist1 > self.arms[dir1] or dist2 > self.arms[dir2]:
			# One of the nodes does not belong to the same arm
			# or they have the wrong distance
			return False

		# Clear current selection
		self.deselect_edge()

		# This edge exists
		# Delete all children associated to this edge
		edge_idx = min(dist1, dist2)
		edge_dir = max(dir1, dir2)
		# Recalculate the shape if something changed
		if self._remove_children_from_edge(edge_idx, edge_dir):
			self.recalculate_shape()
			self.recalculate_parent_shapes()
		
		return True


	# This only clears the children from the edge
	def _remove_children_from_edge(self, edge_idx, edge_dir):
		removed_children = False
		children = []
		removed_sfs = set()
		for dist, rot, sf in self.children[edge_dir]:
			if dist == edge_idx:
				# Remove the child
				removed_children = True
				removed_sfs.add(sf)
			else:
				# Keep the child
				children.append((dist, rot, sf))
		# If something changed: Update child list and recalculate the shape
		if removed_children:
			self.children[edge_dir] = children
			# Tell the removed children we are no longer their parent
			children = self.get_children()
			for child in removed_sfs:
				if child not in children:
					child.remove_parent(self)
			return True
		return False


	def handle_rotate(self, left):
		if self.selected and self.selected_edge != None and self.child_candidate != None:
			# Regenerate child preview shape
			self.child_rot = (self.child_rot + (1 if left else 5)) % 6
			self._gen_child_preview()


	def _gen_child_preview(self):
		start_node = self.nodes[self.edges[self.selected_edge][self.selected_edge_start]]
		self.preview_shape = self.child_candidate.gen_shifted_shape((self.pos[0] + start_node[0], self.pos[1] + start_node[1]), self.child_rot, self.selected_edge_dir)


	def handle_confirm(self):
		if self.selected and self.selected_edge != None and self.child_candidate != None:
			# Merge the preview shape into our shape
			self.merge_with(self.preview_shape)
			# Register the child
			direction, dist = self._test_node_arm(self.nodes[self.edges[self.selected_edge][1 - self.selected_edge_start]], False)
			rot = self.child_rot
			child = (dist - 1, rot, self.child_candidate)
			# First check if we already have this child
			duplicate = False
			for c in self.children[direction]:
				if c == child:
					duplicate = True
					break
			if not duplicate:
				self.children[direction].append(child)
				# Add ourselves as parent
				self.child_candidate.add_parent(self)
			# Deselect the other snowflake and our edge
			self.deselect_edge()
			# Update our arm info
			self._update_arms()
			# Update all parents
			self.recalculate_parent_shapes()


	# Recalculates shape based on arms and children
	def recalculate_shape(self):
		# Clear everything
		self.nodes = [(0, 0)]
		self.nodes_tf = []
		self.edges = []
		self.faces = []

		# Generate nodes and edges from arm info
		for direction in range(6):
			vec = DIR_VECS[direction]
			for dist in range(1, self.arms[direction] + 1):
				# Create the new node
				node = (vec[0] * dist, vec[1] * dist)
				idx = len(self.nodes)
				self.nodes.append(node)
				prev_idx = 0 if dist == 1 else idx - 1
				self.edges.append((prev_idx, idx))
		self.recalculate_points()

		# Merge with every child shape
		for direction in range(6):
			vec = DIR_VECS[direction]
			for dist, rot, child in self.children[direction]:
				# Generate the shifted shape
				pos = (self.pos[0] + vec[0] * dist, self.pos[1] + vec[1] * dist)
				shape = child.gen_shifted_shape(pos, rot, direction)
				# Merge the shape into ours
				self.merge_with(shape)
				self._update_arms()


	def recalculate_parent_shapes(self):
		# First, get all parents (also transitive ones)
		parents = self.get_parents_recursive()
		# Recalculate every parent that does not have a child among these parents
		while len(parents) > 0:
			for p in parents:
				children = p.get_children_recursive()
				if len(children.intersection(parents)) > 0:
					continue
				# The parent has no children in the set of parents!
				break
			parents.remove(p)
			p.recalculate_shape()


	# Only extends arms, does not trim them
	def _update_arms(self):
		for direction in range(6):
			vec = DIR_VECS[direction]
			dist = 0
			prev_idx = 0
			while True:
				# Check if we have the next node and the edge to it
				next_node = (vec[0] * (dist + 1), vec[1] * (dist + 1))
				next_idx = -1
				for i in range(len(self.nodes)):
					if self.nodes[i] == next_node:
						next_idx = i
						break
				if next_idx == -1:
					# We do not have this node
					break
				if not self.contains_edge((prev_idx, next_idx)):
					# We do not have this edge
					break
				# We have the edge and need to continue
				prev_idx = next_idx
				dist += 1
			self.arms[direction] = dist


	# Returns the arm direction and distance if the node is on one of our arms,
	# otherwise -1, 0
	def _test_node_arm(self, node, is_global = True):
		direction = -1
		dist = -1

		# Convert to local coordinates
		if is_global:
			node = (node[0] - self.pos[0], node[1] - self.pos[1])

		# Check if the node is on any axis
		if node[1] == 0:
			# Horizontal axis
			direction = 0 if node[0] >= 0 else 3
			dist = abs(node[0])
		elif node[0] == 0:
			# NNE axis
			direction = 1 if node[1] >= 0 else 4
			dist = abs(node[1])
		elif node[0] + node[1] == 0:
			# NNW axis
			direction = 2 if node[1] >= 0 else 5
			dist = abs(node[0])

		return direction, dist


class StarConvex(Shape):
	def __init__(self, pos):
		super().__init__(pos, [(0, 0)], [], [], COLOR_NODE, COLOR_EDGE, COLOR_FACE)
		self.constituents = []


	def to_json(self, pretty=False):
		self.gen_constituents()
		s = json.dumps(
		{
			"shape":
			{
				"nodes": [{"x": n[0], "y": n[1]} for n in self.nodes],
				"edges": [{"u": e[0], "v": e[1]} for e in self.edges],
				"faces": [{"u": f[0], "v": f[1], "w": f[2]} for f in self.faces]
			},
			"constituents": self.constituents,
			"dependencyTree": []
		},
		indent=4 if pretty else None)
		return s


	def draw(self):
		super().draw()

		# Origin
		pos = wts(*gtw(*self.pos))
		size = 15 * cam.zoom
		color = COLOR_ROOT_SEL
		draw_circle(*pos, size, color)


	# Checks whether the given node is part of this shape and returns its index if it is
	# Returns -1 otherwise
	def _get_node_idx(self, node):
		try:
			return self.nodes.index(node)
		except ValueError:
			return -1


	# Adds the given node to the shape and spans the parallelogram from the origin
	# to the node, filling all of its nodes, edges and faces
	def add_node_convex(self, node):
		x, y = node[0] - self.pos[0], node[1] - self.pos[1]
		if (x, y) in self.nodes:
			return

		# Find out in which sector the node is
		sector = -1
		if x > 0 and y >= 0:
			sector = 0
		elif x <= 0 and y > 0 and x + y > 0:
			sector = 1
		elif x <= 0 and y > 0 and x + y <= 0:
			sector = 2
		elif y <= 0 and x < 0:
			sector = 3
		elif x >= 0 and y < 0 and x + y < 0:
			sector = 4
		else:
			sector = 5

		# Find out distances in the two vector directions (rotate point into sector 0)
		xr, yr = rotate_grid_point(x, y, (6 - sector) % 6)
		dist1 = xr
		dist2 = yr
		# Find the vectors by which we will shift positions
		dir1_vec = DIR_VECS[sector]
		dir2_vec = DIR_VECS[(sector + 1) % 6]
		
		dist1 += 1 # For easier indices
		dist2 += 1

		# Now add the whole parallelogram
		for ix in range(dist1):
			x = ix * dir1_vec[0]
			y = ix * dir1_vec[1]
			for iy in range(dist2):
				node = (x, y)
				idx = self._get_node_idx(node)
				if idx == -1:
					idx = len(self.nodes)
					self.nodes.append(node)

				# Add edges
				nbr_down = None
				nbr_left = None
				nbr_up = None
				nbr_down_idx = -1
				nbr_left_idx = -1
				nbr_up_idx = -1

				if iy > 0:
					# Edge to previous node
					nbr_down = (x - dir2_vec[0], y - dir2_vec[1])
					nbr_down_idx = self._get_node_idx(nbr_down)
					if nbr_down_idx != -1:
						edge = (nbr_down, node)
						self.add_edge(edge)

				if ix > 0:
					# Edge to left node
					nbr_left = (x - dir1_vec[0], y - dir1_vec[1])
					nbr_left_idx = self._get_node_idx(nbr_left)
					if nbr_left_idx != -1:
						edge = (nbr_left, node)
						self.add_edge(edge)

					# Edge to left upper neighbor
					if iy < dist2 - 1:
						nbr_up = (nbr_left[0] + dir2_vec[0], nbr_left[1] + dir2_vec[1])
						nbr_up_idx = self._get_node_idx(nbr_up)
						if nbr_up_idx != -1:
							edge = (nbr_up, node)
							self.add_edge(edge)

				# Add faces
				if nbr_down_idx != -1 and nbr_left_idx != -1:
					face = (nbr_left, nbr_down, node)
					self.add_face(face)
				if nbr_left_idx != -1 and nbr_up_idx != -1:
					face = (nbr_left, node, nbr_up)
					self.add_face(face)


				x += dir2_vec[0]
				y += dir2_vec[1]

		self.recalculate_points()


	# Adds the end points of the given edge to the shape, spans their parallelograms
	# and adds the edge with the incident face if necessary
	def add_edge_convex(self, edge):
		# First add the two nodes
		self.add_node_convex(edge[0])
		self.add_node_convex(edge[1])

		n1 = self._get_node_idx(edge[0])
		n2 = self._get_node_idx(edge[1])
		if not self.contains_edge((n1, n2)):
			# Must add this edge and face
			self.edges.append((n1, n2))
			# Find the third point to add the face
			vec = (edge[1][0] - edge[0][0], edge[1][1] - edge[0][1])
			left = rotate_grid_point(vec[0], vec[1], 1)
			right = rotate_grid_point(vec[0], vec[1], 5)
			left = (edge[0][0] + left[0], edge[0][1] + left[1])
			right = (edge[0][0] + right[0], edge[0][1] + right[1])
			# Left and right are the two candidates
			# We already contain one of them
			idx = self._get_node_idx(left)
			if idx == -1:
				idx = self._get_node_idx(right)
			self.faces.append((n1, n2, idx))


	# Adds all end points of the given face to the shape and spans their
	# parallelograms
	def add_face_convex(self, face):
		self.add_edge_convex((face[0], face[1]))
		self.add_edge_convex((face[0], face[2]))
		self.add_edge_convex((face[1], face[2]))


	# Generates the constituent shapes of this star convex shape
	def gen_constituents(self):
		self.constituents = []
		# Do this for each sector
		for sector in range(6):
			vec1 = DIR_VECS[sector]
			vec2 = DIR_VECS[(sector + 1) % 6]
			vec3 = DIR_VECS[(sector + 2) % 6]
			vec4 = DIR_VECS[(sector + 3) % 6]

			# Start by moving along the dir1 line and finding the last point with an edge to the "top side"
			# This allows us to find a start point for the outer boundary traversal and detect a line "parallelogram" in this direction
			last_edge_idx = 0
			last_edge_found = False
			node = (0, 0)
			nbr_top_1 = vec2
			nbr_top_2 = (vec1[0] + vec2[0], vec1[1] + vec2[1])
			idx = 0
			while True:
				# Go forward one step
				node = (node[0] + vec1[0], node[1] + vec1[1])
				node_idx = self._get_node_idx(node)
				if node_idx == -1:
					break

				# Step was successful
				idx += 1
				# Check if we have a neighbor
				if not last_edge_found:
					idx_top_1 = self._get_node_idx(nbr_top_1)
					idx_top_2 = self._get_node_idx(nbr_top_2)
					if idx_top_1 != -1 and self.contains_edge((node_idx, idx_top_1)):
						last_edge_idx = idx
					elif idx_top_2 != -1 and self.contains_edge((node_idx, idx_top_2)):
						last_edge_idx = idx
					else:
						last_edge_found = True

				nbr_top_1 = nbr_top_2
				nbr_top_2 = (nbr_top_1[0] + vec1[0], nbr_top_1[1] + vec1[1])
			
			# We now know how long the line in this direction is
			node = (idx * vec1[0], idx * vec1[1])
			node_idx = self._get_node_idx(node)

			# If the last edge occurs earlier and there is no neighbor in the bottom direction, we have to add a parallelogram for the line
			if idx > last_edge_idx:
				# Check if there is a bottom neighbor
				nbr_bot_1 = (node[0] - vec2[0], node[1] - vec2[1])
				nbr_bot_2 = (nbr_bot_1[0] + vec1[0], nbr_bot_1[1] + vec1[1])
				idx_bot_1 = self._get_node_idx(nbr_bot_1)
				idx_bot_2 = self._get_node_idx(nbr_bot_2)
				if (idx_bot_1 == -1 or not self.contains_edge((node_idx, idx_bot_1))) and\
						(idx_bot_2 == -1 or not self.contains_edge((node_idx, idx_bot_2))):
					# Have to add line!
					self.constituents.append({
						"shapeType": 1,
			            "directionW": sector * 2,
			            "directionH": ((sector + 1) % 6) * 2,
			            "a": idx,
			            "d": 0,
			            "c": 0,
			            "a2": 0,
			            "a3": 0
					})

			# Now start at the last point with a neighbor edge
			idx = last_edge_idx
			node = (idx * vec1[0], idx * vec1[1])
			node_idx = self._get_node_idx(node)

			if idx == 0:
				continue

			# Now, we start a traversal at the last position at which an edge was detected
			# We move around the outer boundary of the shape until we reach the border of the next sector
			# Every time we encounter a corner, we change our traversal state until we have identified a
			# new constituent shape

			# Find initial direction
			# There are only 3 directions in which we can go
			direction = -1	# Directions 0, 1, 2 mean the 3 possible directions in counterclockwise order
			directions = [vec2, vec3, vec4]
			angle = -1		# Angle 0 means obtuse, angle 1 means acute
			
			# These are our 3 possible neighbors and their indices
			nbrs = [(node[0] + d[0], node[1] + d[1]) for d in directions]
			idxs = [self._get_node_idx(nbrs[i]) for i in range(3)]

			if idxs[0] != -1 and self.contains_edge((node_idx, idxs[0])):
				direction = 0
				angle = 0
			elif idxs[1] != -1 and self.contains_edge((node_idx, idxs[1])):
				direction = 1
				angle = 1
			else:
				raise ValueError("Traversal failed, found no initial direction")

			# We record the distances and angles
			angles = [angle]
			distances = [idx]
			dist = 0

			# Follow the direction until the next corner
			while True:
				# Find our new node and neighbors
				dir_vec = directions[direction]
				new_node = (node[0] + dir_vec[0], node[1] + dir_vec[1])
				new_idx = self._get_node_idx(new_node)
				dist += 1

				# Check if we have arrived at the sector border
				if rotate_grid_point(new_node[0], new_node[1], (6 - sector) % 6)[0] <= 0:	# Will reach 0 at some point
					# Have reached the next sector: Add the last constituent shape here
					# UNLESS the last shape was already finished, in which case we have no angles
					if direction != 2:
						angles.append(1)
						distances.append(dist)
						self.constituents.append(self._construct_constituent(sector, angles, distances))
					break

				new_nbrs = [(nbrs[i][0] + dir_vec[0], nbrs[i][1] + dir_vec[1]) for i in range(3)]
				new_nbr_idxs = [self._get_node_idx(new_nbrs[i]) for i in range(3)]

				num_nbrs = len([i for i in new_nbr_idxs if i != -1 and self.contains_edge((new_idx, i))])
				# Check if we have arrived at a corner
				if direction == 0 and num_nbrs < 3:
					# Corner while traveling in direction 0
					if num_nbrs == 2:
						# Obtuse corner
						# We have to continue moving then
						angles.append(0)
						distances.append(dist)
						dist = 0
						direction = 1
					else:
						# Acute corner => The constituent shape is a parallelogram
						angles.append(1)
						distances.append(dist)
						dist = 0
						# Construct shape
						self.constituents.append(self._construct_constituent(sector, angles, distances))
						# Now move in direction 2 next
						angles = []
						distances = []
						direction = 2
					
				elif direction == 1 and num_nbrs != 2:
					# Corner while traveling in direction 1
					angles.append(0)
					distances.append(dist)
					# Construct shape
					self.constituents.append(self._construct_constituent(sector, angles, distances))
					angles = []
					distances = []
					dist = 0

					# Decide next distance depending on corner type
					if num_nbrs < 2:
						# Obtuse corner, just continue traveling
						direction = 2
					else:
						# Inverted corner, start new shape
						angles.append(0)
						x, y = rotate_grid_point(new_node[0], new_node[1], (6 - sector) % 6)
						distances.append(x)
						dist = y
						direction = 0

				elif direction == 2 and num_nbrs > 1:
					# Corner while traveling in direction 2
					# In any case: Start a new shape here
					angles.append(0)
					x, y = rotate_grid_point(new_node[0], new_node[1], (6 - sector) % 6)
					distances.append(x)
					dist = y
					# Decide in which direction to go
					if num_nbrs == 2:
						angles.append(0)
						distances.append(dist)
						dist = 0
						direction = 1
					else:
						direction = 0

				node = new_node
				node_idx = new_idx
				nbrs = new_nbrs
				idxs = new_nbr_idxs


	def _construct_constituent(self, sector, angles, distances):
		shape_type = -1
		dir_w = sector * 2
		dir_h = ((sector + 1) % 6) * 2
		a = 0
		d = 0
		c = 0
		a2 = 0
		a3 = 0
		if angles[0] == 1:
			# Triangle or trapezoid
			if angles[1] == 1:
				# Triangle
				shape_type = TRIANGLE
				a = distances[0]
				# print("TRIANGLE: " + str(a))
			else:
				# Trapezoid
				shape_type = TRAPEZOID
				a = distances[0]
				d = distances[1]
				# print("TRAPEZOID: " + str(a) + ", " + str(d))
		else:
			# Parallelogram, inverted trapezoid or pentagon
			if angles[1] == 1:
				# Parallelogram
				shape_type = PARALLELOGRAM
				a = distances[0]
				d = distances[1]
				# print("PARALLELOGRAM: " + str(a) + ", " + str(d))
			elif angles[2] == 1:
				# Trapezoid (inverted)
				shape_type = TRAPEZOID
				d = distances[0]
				a = distances[0] + distances[1]
				dir_w, dir_h = dir_h, dir_w
				# print("INVERTED TRAPEZOID: " + str(a) + ", " + str(d))
			else:
				# Pentagon
				shape_type = PENTAGON
				a = distances[0]
				d = distances[1] + distances[2]
				c = distances[1]
				a2 = a + c
				a3 = a + 1
				# print("PENTAGON: " + str(a) + ", " + str(d) + ", " + str(c) + ", " + str(a2) + ", " + str(a3))

		shape = {
			"shapeType": shape_type,
            "directionW": dir_w,
            "directionH": dir_h,
            "a": a,
            "d": d,
            "c": c,
            "a2": a2,
            "a3": a3
		}
		return shape





# Example shape
# shape = Shape(
# 	(2, 0),											# Position
# 	[(0, 0), (1, 0), (0, 1), (1, 1)],				# Nodes
# 	[(0, 1), (0, 2), (1, 2), (1, 3), (2, 3)],		# Edges
# 	[(0, 1, 2), (1, 2, 3)],							# Faces
# 	BLUE, (0, 90, 255), (0, 128, 255, 128)
# )


# The shape to be edited in basic mode
shape = Shape((0, 0), [(0, 0)], [], [], COLOR_NODE, COLOR_EDGE, COLOR_FACE)
# The snowflake to be edited in snowflake mode
snowflake = Snowflake((0, 0))
# List of all current snowflakes
snowflakes = [snowflake]
# The snowflake that is currently selected
selected_sf = None
sc_shape = StarConvex((0, 0))


# Two modes:
# Basic: Have a single shape that can be edited freely
# Snowflake: Have one root snowflake, can create and edit multiple sub-snowflakes according to recursive definition
# Star convex: Have a single star convex shape to which you can only add elements. The constituent shapes are generated automatically
MODE_BASIC = 0
MODE_SNOWFLAKE = 1
MODE_STAR_CONVEX = 2

mode = MODE_SNOWFLAKE
print("Snowflake Mode")


# Default snowflake shape(s)
# Each element of the list describes one shape as (pos, arms, children)
#     pos is a global grid position (x, y)
#     arms are the arm lengths for the 6 directions
#     children is a list of elements (direction, distance, rotation, child_idx)
# Order must be such that shapes can be built from the last to the first element
shape_descr = [
	((0, 0), [8, 0, 0, 0, 0, 0], [(0, 4, 1, 1)]),
	((-3, -2), [4, 0, 0, 0, 0, 0], [])
]


# Generate default shapes from the description
shapes = [Snowflake(pos) for (pos, _, _) in shape_descr]
for i in range(len(shape_descr)):
	pos, arms, children = shape_descr[i]
	s = shapes[i]
	s.arms = arms
	s.recalculate_shape()
	for (direction, distance, rotation, child_idx) in children:
		# Find the edge nodes
		vec = DIR_VECS[direction]
		n1 = (vec[0] * distance, vec[1] * distance)
		n2 = (vec[0] * (distance + 1), vec[1] * (distance + 1))
		i, j = -1, -1
		for k in range(len(s.nodes)):
			node = s.nodes[k]
			if node == n1:
				i = k
			if node == n2:
				j = k
		# Find the edge index
		edge_idx = -1
		for k in range(len(s.edges)):
			edge = s.edges[k]
			if edge == (i, j) or edge == (j, i):
				edge_idx = k
				break
		# Append the child
		s.children[direction].append((edge_idx, rotation, shapes[child_idx]))
		shapes[child_idx].add_parent(s)
# Recalculate all shapes
for i in range(len(shapes) - 1, -1, -1):
	shapes[i].recalculate_shape()

snowflake = shapes[0]
snowflakes = shapes



# Main loop
while True:
	# Background
	screen.displaysurf.fill(WHITE)
	draw_background()


	# Get the mouse position and find out which element is currently highlighted
	x, y = pygame.mouse.get_pos()
	hle = find_highlighted_element(x, y)


	# Render the shape first to avoid lagging artifacts
	if mode == MODE_BASIC:
		draw_highlighted_element(hle)
		shape.draw()
	elif mode == MODE_SNOWFLAKE:
		# Draw snowflake highlights if one is selected
		if selected_sf != None:
			selected_sf.draw_highlight(hle)
		for sf in snowflakes:
			sf.draw()
	elif mode == MODE_STAR_CONVEX:
		draw_highlighted_element(hle)
		sc_shape.draw()


	# Handle events
	for event in pygame.event.get():
		if event.type == QUIT or event.type == KEYDOWN and event.key == KEY_QUIT:
			pygame.quit()
			sys.exit()

		# Switching modes
		elif event.type == KEYDOWN and event.key == K_TAB:
			shape = Shape((0, 0), [(0, 0)], [], [], COLOR_NODE, COLOR_EDGE, COLOR_FACE)
			snowflake = Snowflake((0, 0))
			snowflakes = [snowflake]
			selected_sf = None
			sc_shape = StarConvex((0, 0))
			cam.x = 0
			cam.y = 0
			if mode == MODE_BASIC:
				mode = MODE_SNOWFLAKE
				print("Snowflake Mode")
			elif mode == MODE_SNOWFLAKE:
				mode = MODE_STAR_CONVEX
				print("Star Convex Shape Mode")
			else:
				mode = MODE_BASIC
				print("Basic Mode")

		# Copying to clipboard
		elif event.type == KEYDOWN and event.key == KEY_CLIPBOARD:
			if mode == MODE_BASIC:
				s = shape.to_json(pretty=False)
			elif mode == MODE_SNOWFLAKE:
				if selected_sf != None:
					s = selected_sf.to_json(pretty=False)
				else:
					s = ""
			elif mode == MODE_STAR_CONVEX:
				s = sc_shape.to_json(pretty=False)
			pygame.scrap.put(pygame.SCRAP_TEXT, bytes(s, "UTF-8"))

		# Saving to JSON file
		elif event.type == KEYDOWN and event.key == KEY_SAVE:
			if mode == MODE_BASIC:
				s = shape.to_json(pretty=True)
				savefile = SAVE_FILE_BASE
			elif mode == MODE_SNOWFLAKE:
				if selected_sf != None:
					s = selected_sf.to_json(pretty=True)
				else:
					s = ""
				savefile = SAVE_FILE_SNOWFLAKE
			elif mode == MODE_STAR_CONVEX:
				s = sc_shape.to_json(pretty=True)
				savefile = SAVE_FILE_STAR_CONVEX
			with open(savefile, "w") as f:
				f.write(s)

		# Printing
		elif event.type == KEYDOWN and event.key == KEY_PRINT:
			if mode == MODE_BASIC:
				shape.print_shape()
			elif mode == MODE_SNOWFLAKE:
				if selected_sf != None:
					selected_sf.print_shape()
			elif mode == MODE_STAR_CONVEX:
				sc_shape.print_shape()

		# Camera movement
		elif event.type == MOUSEBUTTONDOWN and event.button == DRAG_BUTTON:
			cam.dragging = True
			cam.last_mouse_x, cam.last_mouse_y = event.pos

		elif event.type == MOUSEBUTTONUP and event.button == DRAG_BUTTON:
			cam.dragging = False

		elif event.type == MOUSEMOTION and cam.dragging:
			mouse_x, mouse_y = event.pos
			offset_x = (mouse_x - cam.last_mouse_x) / cam.zoom
			offset_y = (mouse_y - cam.last_mouse_y) / cam.zoom
			cam.x -= offset_x
			cam.y += offset_y
			cam.last_mouse_x, cam.last_mouse_y = mouse_x, mouse_y

		# Zoom
		elif event.type == MOUSEWHEEL and not cam.dragging:
			cam.update_zoom(event.y)

		# Resize
		elif event.type == VIDEORESIZE:
			screen.change_size(event.w, event.h)

		# Mode input
		if mode == MODE_BASIC:
			# Placing objects
			if event.type == MOUSEBUTTONDOWN and event.button == PLACE_BUTTON:
				# Determine whether or not it should be removed
				if pygame.key.get_pressed()[KEY_REMOVE]:
					# Try removing an object
					if hle[0] == 0:
						shape.remove_node(hle[1])
					elif hle[0] == 1:
						shape.remove_edge(hle[1])
					elif hle[0] == 2:
						shape.remove_face(hle[1])
				else:
					# Try adding an object to the shape
					if hle[0] == 1:
						shape.add_edge(hle[1])
					elif hle[0] == 2:
						shape.add_face(hle[1])
		elif mode == MODE_SNOWFLAKE:
			if event.type == MOUSEBUTTONDOWN and event.button == PLACE_BUTTON:
				# Some location has been clicked
				# If no snowflake is currently selected: Try selecting one
				if selected_sf == None:
					if hle[0] == 0:
						clicked = False
						# Node has been clicked
						for sf in snowflakes:
							if hle[1] == sf.pos:
								# Delete snowflake if delete button is pressed, otherwise select snowflake
								if pygame.key.get_pressed()[KEY_REMOVE]:
									# Only remove if it is not the main snowflake
									if sf != snowflake:
										sf.remove()
										snowflakes.remove(sf)
								else:
									sf.select()
									selected_sf = sf
								clicked = True
								break
						if not clicked:
							# No snowflake was clicked (at its center)
							# Add new snowflake in case the node has been clicked with Shift pressed
							if pygame.key.get_pressed()[KEY_ADD]:
								sf = Snowflake(hle[1])
								sf.select()
								selected_sf = sf
								snowflakes.append(sf)
				else:
					# Snowflake is currently selected
					if pygame.key.get_pressed()[KEY_REMOVE]:
						clicked = selected_sf.handle_remove_click(hle)
					else:
						clicked = selected_sf.handle_click(hle, snowflakes)
					if not clicked:
						selected_sf.deselect()
						selected_sf = None
			elif event.type == KEYDOWN and selected_sf != None and selected_sf.selected_edge != None and selected_sf.child_candidate != None:
				# Pressed button while having selected edge and child candidate
				if event.key == KEY_LEFT:
					selected_sf.handle_rotate(True)
				elif event.key == KEY_RIGHT:
					selected_sf.handle_rotate(False)
				elif event.key == KEY_CONFIRM:
					selected_sf.handle_confirm()
			# FOR DEBUGGING
			elif event.type == KEYDOWN and selected_sf != None and event.key == K_SPACE:
				selected_sf.recalculate_shape()
			# Moving shape around
			elif event.type == KEYDOWN and selected_sf != None and selected_sf != snowflake and event.key in MOVE_KEYS:
				k = event.key
				selected_sf.move_pos(
					1 if k == KEY_MOVE_R else (-1 if k == KEY_MOVE_L else 0),
					1 if k == KEY_MOVE_U else (-1 if k == KEY_MOVE_D else 0)
				)
		elif mode == MODE_STAR_CONVEX:
			# Placing objects
			if event.type == MOUSEBUTTONDOWN and event.button == PLACE_BUTTON:
				# Don't do anything if remove button is held
				if pygame.key.get_pressed()[KEY_REMOVE]:
					pass
				else:
					# Try adding an object to the shape
					if hle[0] == 0:
						sc_shape.add_node_convex(hle[1])
					elif hle[0] == 1:
						sc_shape.add_edge_convex(hle[1])
					elif hle[0] == 2:
						sc_shape.add_face_convex(hle[1])


	pygame.display.update()
	clock.tick(FPS)
