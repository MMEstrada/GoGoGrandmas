extends Sprite3D

func _ready():
	self.set_process(true)

func _process(delta):
	pass


func _on_Timer_timeout():
	if (get_frame() == (get_vframes()*get_hframes()) - 1):
		set_frame(0)
	else:
		set_frame(get_frame() + 1)
	#print (get_frame())
