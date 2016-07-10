extends KinematicBody

var movement_vector = Vector3(0,0,0)
var last_move = Vector3(0,0,0)
var speed = 1
var jump = 5
var grounded = false

func _ready():
	set_fixed_process(true)
func _fixed_process(delta):
	movement_vector = Vector3()
	if (Input.is_action_pressed("P1_left")):
		movement_vector += Vector3(-speed,0,0)
	if (Input.is_action_pressed("P1_right")):
		movement_vector += Vector3(speed,0,0)
	if (Input.is_action_pressed("P1_up")):
		movement_vector += Vector3(0,0,-speed)
	if (Input.is_action_pressed("P1_down")):
		movement_vector += Vector3(0,0,speed)
	var n = get_collision_normal()
	if (Input.is_action_pressed("P1_jump") and grounded):
				movement_vector += Vector3(0,jump,0)
				grounded = false
	if (!grounded):
		movement_vector += Vector3(0, max(-5, last_move.y-9.81*delta), 0)
	
	if(is_colliding()):
		if (rad2deg(acos(n.dot(Vector3(0, 1, 0)))) < 30):
			# If angle to the "up" vectors is < angle tolerance,
			# char is on floor
			grounded = true
			movement_vector.y = 0
		#if (get_collider_velocity() == Vector3()):
		#	if (Input.is_action_pressed("P1_jump") and grounded):
		#		movement_vector += Vector3(0,jump,0)
		#	else:
		#		movement_vector.y = 0
		else:
			grounded = false
		
	
	#movement_vector = n.slide(movement_vector)
	move(movement_vector)
	last_move = movement_vector
	print(movement_vector)
	print(grounded)
func _input(event):
	pass