extends KinematicBody

var movement_vector = Vector3()
var last_move = Vector3()
var speed = 1
var jump = 2
var grounded = false
var controllable = true
var attacking = false
var jumping = false
var groundn = Vector3()

func _ready():
	set_fixed_process(true)
func _fixed_process(delta):
	if (is_colliding()):
		groundn = get_collision_normal()
	else:
		groundn = Vector3()
	movement_vector = Vector3()
	grounded = GroundCheck(groundn)
	move(MovementManagement(delta))
	ActionManagement()

	
	last_move = movement_vector
	#print(movement_vector)
	print("Grounded: ", grounded)
	print("Colliding: ", is_colliding())
	print("Ground Normal: ", groundn)

func _input(event):
	pass

func MovementManagement(delta):
	if (Input.is_action_pressed("P1_left")):
		movement_vector += Vector3(-speed,0,0)
	if (Input.is_action_pressed("P1_right")):
		movement_vector += Vector3(speed,0,0)
	if (Input.is_action_pressed("P1_up")):
		movement_vector += Vector3(0,0,-speed)
	if (Input.is_action_pressed("P1_down")):
		movement_vector += Vector3(0,0,speed)
	if (is_colliding()):
		groundn = get_collision_normal()
	else:
		groundn = null
	if (Input.is_action_pressed("P1_jump") and grounded and !jumping):
		movement_vector += Vector3(0,jump,0)
		grounded = false
		jumping = true
	if (!grounded):
		movement_vector += Vector3(0, max(-5, last_move.y-9.81*delta), 0)
	#movement_vector.x = groundn.slide(movement_vector).x
	#movement_vector.z = groundn.slide(movement_vector).z
	if (!jumping and grounded and groundn != null):
		movement_vector = groundn.slide(movement_vector)
	return movement_vector

func ActionManagement():
	pass
func GroundCheck(n):
	var ray = get_node("RayCast")
	print (ray.get_name())
	var g = grounded
	if(is_colliding()):
		if (rad2deg(acos(n.dot(Vector3(0, 1, 0)))) < 60):
			# If angle to the "up" vectors is < angle tolerance,
			# char is on floor
			g = true
	#	else:
			#g = false
		
	#else:
	#	g = false
	
	if (ray.is_colliding()):
		print("Hit at point: ",ray.get_collision_point())
	else:
		 g = false
	if (g):
		jumping = false
	return g