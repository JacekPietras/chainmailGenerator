using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothTransporter : MonoBehaviour {
    private Cloth listOfCloth;
    public GameObject receiver;

    // Use this for initialization
    void Start() {
        listOfCloth = (Cloth)GetComponents(typeof(Cloth))[0];
    }

    // Update is called once per frame
    void Update() {
        if (listOfCloth != null && receiver != null) {
            LayerDynamic layer = (LayerDynamic)receiver.GetComponents(typeof(LayerDynamic))[0];
            layer.sendVerticles(listOfCloth.vertices);
        }
    }
}
