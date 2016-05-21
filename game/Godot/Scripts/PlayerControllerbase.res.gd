extends PhysicsBody

var movement_vector = Vector3(0,0,0)
var last_move = Vector3(0,0,0)
var speed = 5

func _ready():
	set_process(true)
func _process(delta):
	if (Input.is_mouse_button_pressed(1)):
		set_axis_lock(get_axis_lock()+1)
	movement_vector = Vector3(0,0,0)
	if (Input.is_action_pressed("P1_left")):
		movement_vector += Vector3(-speed,0,0)
	if (Input.is_action_pressed("P1_right")):
		movement_vector += Vector3(speed,0,0)
	if (Input.is_action_pressed("P1_up")):
		movement_vector += Vector3(0,0,-speed)
	if (Input.is_action_pressed("P1_down")):
		movement_vector += Vector3(0,0,speed)
	if (Input.is_action_pressed("P1_jump")) :#and (last_move.y == 0)):
		movement_vector += Vector3(0,speed,0)
	else:
		movement_vector += Vector3(0, max(-15, last_move.y-9.81), 0)
	set_linear_velocity(movement_vector)
	last_move = get_linear_velocity()
	set_angular_velocity(Vector3(0,0,0))
	print(get_axis_lock())
func _integrate_forces(state):
	pass
func _input(event):
	pass