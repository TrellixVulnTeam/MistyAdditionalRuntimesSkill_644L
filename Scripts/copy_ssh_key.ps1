# allow the user to override the robot's IP address
$ip='10.0.1.111'
if (Test-Path env:ROBOT_IP) {
    $ip=${env:ROBOT_IP}
}

# right now administrator is the only supported option but it could be something else
$user='administrator'

# assume that the user has a public key here
$local_pub_key="${env:USERPROFILE}\.ssh\id_rsa.pub"
$local_drive_name='X:'

$target_share="\\${ip}\c$"

$target_keys="${local_drive_name}\Data\ProgramData\ssh\administrators_authorized_keys"

echo ""
echo "Mounting SMB share from $target_share to $local_drive_name"
net use $local_drive_name $target_share "/USER:${ip}\$user"

# this will destroy and previously pushed SSH keys
# TODO: append the key if it isn't already in the file
echo ""
echo "Copying public key from $local_pub_key to $target_keys"
copy $local_pub_key $target_keys

echo ""
echo "Setting ACL on public key"
icacls $target_keys /remove "NT AUTHORITY\Authenticated Users"
icacls $target_keys /inheritance:r

echo ""
echo "Unmounting SMB share $target_share"
net use $local_drive_name /delete /yes
