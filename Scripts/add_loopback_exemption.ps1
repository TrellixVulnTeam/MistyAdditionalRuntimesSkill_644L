# allow the user to override the robot's IP address
$ip='10.0.1.111'
if (Test-Path env:ROBOT_IP) {
    $ip=${env:ROBOT_IP}
}

# right now administrator is the only supported value but it could be DefaultUser
$user='administrator'

# change this value if you rename this skill
$packageFamily="AdditionalRuntimesSkill-uwp"

# run the copy_ssh_key.ps1 script to copy your SSH public key to the robot
ssh "$user@$ip" "CheckNetIsolation.exe LoopbackExempt -a -n=$packageFamily"
