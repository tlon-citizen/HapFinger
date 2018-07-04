
use <Utilities.scad>;

$fn=75;

//All parameters in mm.
fingerLength    =50.0;
fingerRadius    =12.5;

module finger() {
    union()
    {
        scale([1.0, 1.03, 0.85])
        {
            rotate([0, 90, 0]) 
            {
                cylinder(h=fingerLength-fingerRadius, r=fingerRadius, center=true);
            }
            translate([(fingerLength-fingerRadius)/2, 0.0, 0.0])
            {
                sphere(r=(fingerRadius+0.01), center=true);
            }
        }
    };
}

module fingerCap() {
    difference()
    {
        color("YellowGreen",1.0)
        finger();
        
        scale([1.0, 0.9, 0.85])
            translate([-4.85, 0.0, 0.0])
                color("SteelBlue",1.0)
                finger();
        
        translate([-34.6, 0.0, -25.5]) // Finger tunnel A
            color("DarkOrange", 1.0)
            roundedCube(43.0, 25.0, 25.0, 3.0);
        
        translate([-34.6, -25.0, -31.5]) // Finger tunnel B
            color("DarkOrange", 1.0)
            roundedCube(43.0, 25.0, 25.0, 3.0);
        
        fingerTip();
    }
}

module fingerTip() {
    intersection()
    {
        color("YellowGreen",1.0)
        finger();

        translate([32.075, 0.0, 0.0]) // Tip slice for the pressure sensor
        {
            rotate([90.0, 0.0, 90.0])
            {
                color("DarkOrange",1.0)
                cylinder(h=5.0, r=6.0, center=true);
            }
        }
    }
}

module lra() {
    connectorRadio = 1.6;
    connectorDepth = 2.5;
    translate([22.5, 0.0, -1.15])
    {
        rotate([90.0, 0.0, 90.0])
        {
            color("LightSteelBlue",1.0)
            cylinder(h=6.0, r=5.5, center=true);
            
            translate([0.0, 5.80, 1.70])
            color("Yellow", 1.0)
            hull()
            {
                cube([connectorRadio*2, connectorRadio, connectorDepth], center=true);
                translate([0.0, connectorRadio, 0.0])
                cylinder(h=connectorDepth, r=connectorRadio, center=true);
            };            
        }
    };
}

module componentsBed() {
    translate([-19.6, -12.0, 7.0])
    {
        color("Orange", 1.0)
        roundedCube(40.0, 28.75, 8.0, 3.0);
    }
}

module componentsBox() {
    yPos    =12.75;
    zOffset =-10.0;
    xOffset =0.5;
    
    union()
    {
        difference()
        {
            union()
            {
                componentsBed();
                translate([-20.1+xOffset, yPos, -7.5+zOffset]) // Input Controls Box
                {
                    color("Orange", 1.0)
                    roundedCube(28.0, 4.0, 25.0, 3.0);
                };
                
                //translate([-4.5, 18.1, 7.5]) // HCI Logo
                translate([-4.5, 18.7, 7.5]) // HCI Logo
                {
                    //rotate([90, 180.0, 180,0])
                    rotate([90, 180.0, 0,0])
                    color("Green", 1.0)
                    scale([8.5, 8.5, 9.5])
                    import("hciLogo.stl", convexity=3);
                }
            }
            translate([-19.55, -12.1, 7.0]) //Interior
            {
                color("Gray", 1.0)
                cube([40.0, 29.0, 8.4]);
            }
            
            componentsLid(); // Lid Gutter
            
            translate([-10.5+xOffset, yPos, -7.5+zOffset]) // Joystick Hole
            {
                color("Gray", 1.0)
                cube([18.5, 5.5, 18.5]);
            }

            translate([-18.7+xOffset, yPos, 3.5+zOffset]) // Button A Hole
            {
                color("Gray", 1.0)
                cube([6.5, 7.5, 6.5]);
            }
            translate([-18.7+xOffset, yPos, -6.0+zOffset]) // Button B Hole
            {
                color("Gray", 1.0)
                cube([6.5, 7.5, 6.5]);
            }
            translate([-6.0, 12.75, -2.0]) // Components-Controls Hole
            {
                color("Gray", 1.0)
                cube([10.5, 3.0, 10.0]);
            }
            translate([-18.7+xOffset, 12.75, -1.5+zOffset]) // Inter-Button Channel
            {
                color("Gray", 1.0)
                cube([6.5, 3.0, 20.0]);
            }
        }
        
        joystickCorners(xOffset, yPos, zOffset);
    }
}

module joystickCorners(xOffset, yPos, zOffset)
{
    translate([-10.6+xOffset, 5.5+yPos, 7.2+zOffset]) // Joystick Corner A
    {
        rotate([90, 0.0, 0,0])
        color("Orange", 1.0)
        extrudedTriangle(5.5, 5.55); 
    }
    translate([-6.7+xOffset, 5.5+yPos, -7.6+zOffset]) // Joystick Corner B
    {
        rotate([90, -90.0, 0,0])
        color("Orange", 1.0)
        extrudedTriangle(5.5, 5.5); 
    }
    translate([8.1+xOffset, 0.0+yPos, 7.2+zOffset]) // Joystick Corner C
    {
        rotate([90, 0.0, 180,0])
        color("Orange", 1.0)
        extrudedTriangle(5.5, 5.5); 
    }
    translate([4.2+xOffset, 0.0+yPos, -7.6+zOffset]) // Joystick Corner D
    {
        rotate([90, -90.0, 180,0])
        color("Orange", 1.0)
        extrudedTriangle(5.5, 5.5); 
    }
}

module componentsLid() {
    translate([-16.45, -13.2, 15.9])
    {
        difference()
        {
            union()
            {
                color("SteelBlue", 1.0)
                roundedCube(33.4, 25.0, 0.01, 1.25);
                
                translate([3.7, 9.0, 0.55])
                {
                    color("Yellow", 1.0)
                    arrow();
                }
                translate([3.7, 5.0, 0.55])
                {
                    color("Yellow", 1.0)
                    rotate([0, 0, 180.0])
                    arrow();
                }
            }
        }
    }
}

module channels() {
    translate([20.5, -5.25, 7.25]) // Pressure sensor
    {
        color("Red", 1.0)
        roundedCube(9.0, 10.5, 0.5, 0.5);
    }

    translate([-19.54, -13.5, 8.6]) // On/Off Switch
    {
        color("Red", 1.0)
        cube([9.0, 3.0, 3.0]);
    }
    
    translate([-21.5, -6.1, 8.6]) // Mini-USB-Charger Plug
    {
        color("Red", 1.0)
        cube([3.0, 8.0, 3.0]);
    }

    translate([4.8, -13.5, 7.01]) // Serial Port
    {
        color("Red", 1.0)
        cube([15.5, 3.0, 3.0]);
    }
    
    hole(19.0, -1.5, 16.0); // IR LED Hole A
    hole(19.0,  1.5, 16.0); // IR LED Hole B
}

module fingerCapModel() {
    difference()
    {
        union()
        {
            difference()
            {
                fingerCap();
                lra();
                componentsBed();
            }
            componentsBox();
        }
        channels();
    }
    translate([0.0, -35.0, 0.0])
    {
        componentsLid();
    }
    rotate([0.0, -90.0, 0,0]) 
    {
        translate([-15.0, -60.0, 0.0])
        fingerTip();
    }
}

mirror([1,0,0])
fingerCapModel();
